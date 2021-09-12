using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.ByteOperations;
using static GB_Emulator.Statics.Frequencies;
using static GB_Emulator.Statics.InterruptAddresses;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Gameboi.Hardware
{
    public enum InterruptType
    {
        VBlank,
        LCDC,
        Timer,
        Link,
        Joypad
    }

    public class CPU : Hardware
    {
        private bool IME = true; // Interrupt Master Enable

        private bool shouldUpdateIME = false;
        private bool nextIMEValue = false;

        private readonly InterruptRegister IE = new();
        private readonly InterruptRegister IF = new();

        private bool isHalted = false;

        private readonly Action[] instructions;
        private readonly byte[] durations;
        private readonly Action[] cbInstructions;

        #region Registers
        private byte A; // accumulator
        private byte F; // flag register
        private byte B;
        private byte C;
        private Address BC => ConcatBytes(B, C);
        private byte D;
        private byte E;
        private Address DE => ConcatBytes(D, E);
        private byte H;
        private byte L;
        private Address HL => ConcatBytes(H, L);
        private ushort PC; //progarm counter
        private byte PC_P => GetHighByte(PC);
        private byte PC_C => GetLowByte(PC);
        private ushort SP = 0xFFFE; //stack pointer
        private byte SP_S => GetHighByte(SP);
        private byte SP_P => GetLowByte(SP);

        #endregion


        #region Flags
        const byte zero_bit = 7;
        const byte subtract_bit = 6;
        const byte halfCarry_bit = 5;
        const byte carry_bit = 4;
        public bool ZeroFlag => TestBit(zero_bit, F);
        public bool SubtractFlag => TestBit(subtract_bit, F);
        public bool HalfCarryFlag => TestBit(halfCarry_bit, F);
        public bool CarryFlag => TestBit(carry_bit, F);

        private void SetFlags(bool Z, bool S, bool H, bool C)
        {
            SetZeroFlag(Z);
            SetSubtractFlag(S);
            SetHalfCarryFlag(H);
            SetCarryFlag(C);
        }

        private void SetZeroFlag(bool Z) => SetFlag(zero_bit, Z);
        private void SetSubtractFlag(bool S) => SetFlag(subtract_bit, S);
        private void SetHalfCarryFlag(bool H) => SetFlag(halfCarry_bit, H);
        private void SetCarryFlag(bool C) => SetFlag(carry_bit, C);


        private void SetFlag(int bit, bool on)
        {
            if (on) F = SetBit(bit, F);
            else F = ResetBit(bit, F);
        }
        #endregion

        public override Byte Read(Address address) => bus.Read(address, true);
        public override void Write(Address address, Byte value) => bus.Write(address, value, true);

        private static readonly double ratio = Stopwatch.Frequency / (double)cpuSpeed;

        private Task runner;
        public bool IsRunning { get; private set; }

        public void Run()
        {
            IsRunning = true;
            if (runner == null)
            {
                runner = new Task(Loop);
                runner.Start();
            }
        }

        public void Pause()
        {
            IsRunning = false;
            while (runner.Status != TaskStatus.RanToCompletion)
                Thread.Sleep(1);
            runner.Dispose();
            runner = null;
        }

        public void Restart(bool isColorMode)
        {
            A = (byte)(isColorMode ? 0x11 : 0x01);
            PC = 0x100;
            SP = 0xFFFE;
            F = 0xB0;
            B = 0;
            C = 0x13;
            D = 0;
            E = 0xD8;
            H = 1;
            L = 0x4D;
        }

        private static readonly double frameRate = 60d;
        private static readonly uint cyclesPerFrame = (uint)(cpuSpeed / frameRate);
        private static readonly double tsPerFrame = ratio * cyclesPerFrame;
        private static readonly double tsPerMs = tsPerFrame / 16d;

        public void Loop()
        {
            long elapsed = 0;
            while (IsRunning)
            {
                long startTs = Stopwatch.GetTimestamp();
                ulong start = Cycles;

                bus.AddNextFrameOfSamples();

                while (elapsed < cyclesPerFrame)
                {
                    DoNextInstruction();
                    elapsed = (uint)(Cycles - start);
                }
                elapsed -= cyclesPerFrame;
                long targetTs = startTs + (long)tsPerFrame;
                long remainingTs = targetTs - Stopwatch.GetTimestamp();
                if (remainingTs > 0)
                {
                    double remainingMs = remainingTs / tsPerMs;
                    Thread.Sleep((int)remainingMs);
                }
                while (Stopwatch.GetTimestamp() < targetTs)
                {
                    Thread.SpinWait(1);
                }
            }
        }

        public void DoNextInstruction()
        {
            HandleInterrupts();

            if (isHalted)
            {
                NoOperation();
                bus.UpdateCycles(4);
            }
            else
            {
                // Fetch, Decode, Execute
                byte opCode = Fetch();
                instructions[opCode]();
                bus.UpdateCycles(durations[opCode]);
            }
        }

        #region Interrupts
        private void HandleInterrupts()
        {
            if (shouldUpdateIME)
            {
                IME = nextIMEValue;
                shouldUpdateIME = false;
                return;
            }


            // if IF is 0 there are no interrupt requests => exit
            if (!IF.Any()) return;

            // any interrupt request should remove halt-state (even if events are not enabled)
            isHalted = false;

            if (!IME) return;

            if (!IE.Any()) return;

            // type             :   prio    : address   : bit
            //---------------------------------------------------
            // V-Blank          :     1     : 0x0040    : 0
            // LCDC Status      :     2     : 0x0048    : 1
            // Timer Overflow   :     3     : 0x0050    : 2
            // Serial Transfer  :     4     : 0x0058    : 3
            // Hi-Lo of P10-P13 :     5     : 0x0060    : 4
            if (IE.Vblank && IF.Vblank)
            {
                IF.Vblank = false;
                Interrupt(VblankVector);
            }
            else if (IE.LcdStat && IF.LcdStat)
            {
                IF.LcdStat = false;
                Interrupt(LcdStatVector);
            }
            else if (IE.Timer && IF.Timer)
            {
                IF.Timer = false;
                Interrupt(TimerVector);
            }
            else if (IE.Serial && IF.Serial)
            {
                IF.Serial = false;
                Interrupt(SerialVector);
            }
            else if (IE.Joypad && IF.Joypad)
            {
                IF.Joypad = false;
                Interrupt(JoypadVector);
            }
        }

        private void Interrupt(ushort interruptVector)
        {
            IME = false;
            Call(interruptVector);
            bus.UpdateCycles(24);
        }

        public void RequestInterrupt(InterruptType type)
        {
            switch (type)
            {
                case InterruptType.VBlank:
                    {
                        IF.Vblank = true;
                        break;
                    }
                case InterruptType.LCDC:
                    {
                        IF.LcdStat = true;
                        break;
                    }
                case InterruptType.Timer:
                    {
                        IF.Timer = true;
                        break;
                    }
                case InterruptType.Link:
                    {
                        IF.Serial = true;
                        break;
                    }
                case InterruptType.Joypad:
                    {
                        IF.Joypad = true;
                        break;
                    }
            }
        }

        #endregion


        #region Helpers

        public override void Connect(Bus bus)
        {
            base.Connect(bus);
            bus.ReplaceMemory(IE_address, IE);
            bus.ReplaceMemory(IF_address, IF);
        }

        private byte Fetch()
        {
            //Fetch instruction and increment PC after
            return Read(PC++);
        }

        private ushort GetDirectAddress()
        {
            byte lowByte = Fetch();
            return ConcatBytes(Fetch(), lowByte);
        }
        #endregion


        #region Instructions

        #region Misc
        private void Empty() { }
        private void NoOperation() { }
        private static void Stop(byte _)
        {
            //TODO: display white line in center and do nothing untill any button is pressed. 
        }
        private void Halt()
        {
            // Halts CPU until interrupt happens => Perform NOPs meanwhile to not fuck up memory
            isHalted = true;
        }

        private void DisableInterrupt()
        {
            shouldUpdateIME = true;
            nextIMEValue = false;
        }
        private void EnableInterrupt()
        {
            shouldUpdateIME = true;
            nextIMEValue = true;
        }

        private void Prefix_CB()
        {
            byte opCode = Fetch();
            cbInstructions[opCode]();
            Byte modded = opCode % 8;
            Byte duration = modded == 6 ? 16 : 8;
            bus.UpdateCycles(duration);
        }

        private void SetCarryFlagInstruction()
        {
            SetCarryFlag(true);
            SetHalfCarryFlag(false);
            SetSubtractFlag(false);
        }
        private void ComplementCarryFlag()
        {
            SetSubtractFlag(false);
            SetHalfCarryFlag(false);
            SetCarryFlag(!CarryFlag);
        }
        #endregion

        #region Loads
        private static void Load(ref byte target, byte source)
        {
            target = source;
        }
        private static void Load(ref byte targetHigh, ref byte targetLow, ushort value)
        {
            targetLow = GetLowByte(value);
            targetHigh = GetHighByte(value);
        }
        private static void Load(ref ushort target, ushort value)
        {
            target = value;
        }
        private void LoadToMem(ushort address, byte source)
        {
            Write(address, source);
        }
        private void LoadToMem(ushort address, ushort source)
        {
            Write(address, GetLowByte(source));
            Write((ushort)(address + 1), GetHighByte(source));
        }
        #endregion

        #region Aritmetic
        private void DAA()
        {
            bool setC = CarryFlag;

            if (SubtractFlag)
            {
                if (CarryFlag)
                    A -= 0x60;
                if (HalfCarryFlag)
                    A -= 0x6;
            }
            else
            {
                if (CarryFlag || A > 0x99)
                {
                    A += 0x60;
                    setC = true;
                }
                if (HalfCarryFlag || (A & 0xF) > 0x09)
                {
                    A += 0x6;
                }
            }

            SetCarryFlag(setC);
            SetZeroFlag(A == 0);
            SetHalfCarryFlag(false);
        }

        private void Add(ref byte target, byte operand, bool withCarry = false)
        {
            target = Add8(target, operand, out bool C, out bool H, withCarry);
            SetFlags(target == 0, false, H, C);
        }
        private void Add(ref byte targetHigh, ref byte targetLow, byte operandHigh, byte operandLow)
        {
            ushort target = ConcatBytes(targetHigh, targetLow);
            ushort operand = ConcatBytes(operandHigh, operandLow);
            ushort result = Add16(target, operand, out bool C, out bool H);
            targetHigh = GetHighByte(result);
            targetLow = GetLowByte(result);
            SetSubtractFlag(false);
            SetCarryFlag(C);
            SetHalfCarryFlag(H);
        }

        private void Subtract(ref byte target, byte operand, bool withCarry = false)
        {
            target = Sub8(target, operand, out bool C, out bool H, withCarry);
            SetFlags(target == 0, true, H, C);
        }

        private void Increment(ref byte target)
        {
            Byte low4 = target & 0xF;
            SetHalfCarryFlag(low4 == 0xF); // set if carry from bit 3
            target++;
            SetZeroFlag(target == 0);
            SetSubtractFlag(false);
        }
        private static void Increment(ref byte targetHigh, ref byte targetLow)
        {
            int newLowByte = targetLow + 1;
            if (newLowByte > 0xFF)
            {
                targetHigh++;
            }
            targetLow = (byte)newLowByte;
        }
        private static void Increment(ref ushort target)
        {
            target++;
        }
        private void IncrementInMemory(byte addressHigh, byte addressLow)
        {
            ushort address = ConcatBytes(addressHigh, addressLow);
            byte value = Read(address);
            Increment(ref value);
            Write(address, value);
        }

        private void Decrement(ref byte target)
        {
            Byte low4 = target & 0xF;
            SetHalfCarryFlag(low4 == 0); // set if borrow from bit 4
            target--;
            SetZeroFlag(target == 0);
            SetSubtractFlag(true);
        }
        private static void Decrement(ref byte targetHigh, ref byte targetLow)
        {
            int newLowByte = targetLow - 1;
            if (newLowByte < 0)
            {
                targetHigh--;
            }
            targetLow = (byte)newLowByte;
        }
        private static void Decrement(ref ushort target)
        {
            target--;
        }
        private void DecrementInMemory(byte addresshigh, byte addressLow)
        {
            ushort address = ConcatBytes(addresshigh, addressLow);
            byte value = Read(address);
            Decrement(ref value);
            Write(address, value);
        }

        private void RotateLeftWithCarry(ref byte target, bool cb_mode = true)
        {
            int rotated = target << 1;
            bool isCarry = rotated > 0xFF;
            if (isCarry)
                target = (byte)(rotated | 1); //wrap around carry bit
            else
                target = (byte)rotated; //no need for wrap around
            SetFlags(cb_mode && target == 0, false, false, isCarry);
        }
        private void RotateRightWithCarry(ref byte target, bool cb_mode = true)
        {
            bool isCarry = (target & 1) != 0;
            int rotated = target >> 1;
            if (isCarry)
                target = (byte)(rotated | 0x80);
            else
                target = (byte)rotated;
            SetFlags(cb_mode && target == 0, false, false, isCarry);
        }
        private void RotateLeft(ref byte target, bool cb_mode = true)
        {
            int rotated = target << 1;
            bool oldIsCarry = CarryFlag;
            bool isCarry = rotated > 0xFF;
            if (oldIsCarry)
                target = (byte)(rotated | 1);
            else
                target = (byte)rotated;
            SetFlags(cb_mode && target == 0, false, false, isCarry);
        }
        private void RotateRight(ref byte target, bool cb_mode = true)
        {
            bool oldIsCarry = CarryFlag;
            bool isCarry = (target & 1) != 0;
            int rotated = target >> 1;
            if (oldIsCarry)
                target = (byte)(rotated | 0x80);
            else
                target = (byte)rotated;
            SetFlags(cb_mode && target == 0, false, false, isCarry);
        }

        private void Set(int bit, ushort address)
        {
            byte value = Read(address);
            Set(bit, ref value);
            Write(address, value);
        }
        private static void Set(int bit, ref byte target)
        {
            target = SetBit(bit, target);
        }

        private void Reset(int bit, ushort address)
        {
            byte value = Read(address);
            Reset(bit, ref value);
            Write(address, value);
        }
        private static void Reset(int bit, ref byte target)
        {
            target = ResetBit(bit, target);
        }

        private void Bit(int bit, byte source)
        {
            SetZeroFlag(!TestBit(bit, source));
            SetSubtractFlag(false);
            SetHalfCarryFlag(true);
        }

        private void Swap(ushort address)
        {
            byte value = Read(address);
            Swap(ref value);
            Write(address, value);
        }
        private void Swap(ref byte target)
        {
            target = SwapNibbles(target);
            SetFlags(target == 0, false, false, false);
        }

        private void ShiftLeftA(ushort address)
        {
            byte value = Read(address);
            ShiftLeftA(ref value);
            Write(address, value);
        }
        private void ShiftLeftA(ref byte target)
        {
            int shifted = target << 1;
            target = (byte)shifted;
            SetFlags(target == 0, false, false, shifted > 0xFF);
        }
        private void ShiftRightA(ushort address)
        {
            byte value = Read(address);
            ShiftRightA(ref value);
            Write(address, value);
        }
        private void ShiftRightA(ref byte target)
        {
            bool isCarry = (target & 1) == 1;
            int shifted = target >> 1;
            target = (byte)(shifted | target & 0x80);
            SetFlags(target == 0, false, false, isCarry);
        }
        private void ShiftRightL(ushort address)
        {
            byte value = Read(address);
            ShiftRightL(ref value);
            Write(address, value);
        }
        private void ShiftRightL(ref byte target)
        {
            bool isCarry = (target & 1) == 1;
            int shifted = target >> 1;
            target = (byte)shifted;
            SetFlags(target == 0, false, false, isCarry);
        }

        private void RotateLeft(ushort address)
        {
            byte value = Read(address);
            RotateLeft(ref value);
            Write(address, value);
        }
        private void RotateRight(ushort address)
        {
            byte value = Read(address);
            RotateRight(ref value);
            Write(address, value);
        }
        private void RotateRightWithCarry(ushort address)
        {
            byte value = Read(address);
            RotateRightWithCarry(ref value);
            Write(address, value);
        }
        private void RotateLeftWithCarry(ushort address)
        {
            byte value = Read(address);
            RotateLeftWithCarry(ref value);
            Write(address, value);
        }
        #endregion

        #region Jumps
        private void ConditionalJumpBy(bool condition, byte increment)
        {
            if (condition)
            {
                bus.UpdateCycles(4);
                JumpBy(increment);
            }
        }
        private void JumpBy(byte increment) //actually signed
        {
            PC = (ushort)(PC + (sbyte)increment);
        }

        private void ConditionalJumpTo(bool condition, ushort address)
        {
            if (condition)
            {
                bus.UpdateCycles(4);
                JumpTo(address);
            }
        }
        private void JumpTo(ushort newPC)
        {
            PC = newPC;
        }
        private void ConditionalReturn(bool condition)
        {
            if (condition)
            {
                bus.UpdateCycles(12);
                Return();
            }
        }
        private void Return()
        {
            byte newPCLow = Read(SP++);
            byte newPCHigh = Read(SP++);
            PC = ConcatBytes(newPCHigh, newPCLow);
        }
        private void ReturnAndEnableInterrupt()
        {
            Return();
            EnableInterrupt();
        }
        private void ConditionalCall(bool condition, ushort address)
        {
            if (condition)
            {
                bus.UpdateCycles(12);
                Call(address);
            }
        }
        private void Call(ushort address)
        {
            Push(PC_P, PC_C);
            JumpTo(address);
        }
        private void Restart(byte newPC)
        {
            Push(PC_P, PC_C);
            JumpTo(newPC);
        }
        #endregion

        #region Logic
        private void Complement(ref byte target)
        {
            SetHalfCarryFlag(true);
            SetSubtractFlag(true);
            target = Invert(target);
        }
        private void And(ref byte target, byte operand)
        {
            target &= operand;
            SetFlags(target == 0, false, true, false);
        }
        private void Xor(ref byte target, byte operand)
        {
            target ^= operand;
            SetFlags(target == 0, false, false, false);
        }
        private void Or(ref byte target, byte operand)
        {
            target |= operand;
            SetFlags(target == 0, false, false, false);
        }
        private void Compare(byte target, byte operand)
        {
            byte result = Sub8(target, operand, out bool C, out bool H);
            SetFlags(result == 0, true, H, C);
        }
        #endregion

        #region Stack Interaction
        private void Push(byte high, byte low)
        {
            Write(--SP, high);
            Write(--SP, low);
        }
        private void Pop(ref byte targetHigh, ref byte targetLow, bool isFlagRegister = false)
        {
            targetLow = Read(SP++);
            if (isFlagRegister)
                targetLow &= 0xF0;
            targetHigh = Read(SP++);
        }
        private void AddToStackPointer(byte operand)
        {
            //Set flags by add8 and unsigned operand
            Add8(SP_P, operand, out bool C, out bool H);
            //treat operand as signed when adding to SP
            SP = (ushort)(SP + (sbyte)operand);
            SetFlags(false, false, H, C);
        }
        #endregion

        #endregion


        public CPU()
        {
            //setup normal instructions
            instructions = new Action[0x100]
            {
            // 0x0X
            NoOperation,
            () => Load(ref B, ref C, GetDirectAddress()),
            () => LoadToMem(BC, A),
            () => Increment(ref B, ref C),
            () => Increment(ref B),
            () => Decrement(ref B),
            () => Load(ref B, Fetch()),
            () => RotateLeftWithCarry(ref A, false),
            () => LoadToMem(GetDirectAddress(),SP),
            () => Add(ref H, ref L, B, C),
            () => Load(ref A, Read(BC)),
            () => Decrement(ref B, ref C),
            () => Increment(ref C),
            () => Decrement(ref C),
            () => Load(ref C, Fetch()),
            () => RotateRightWithCarry(ref A, false),



            // 0x1X
            () => Stop(Fetch()),
            () => Load(ref D, ref E, GetDirectAddress()),
            () => LoadToMem(DE, A),
            () => Increment(ref D, ref E),
            () => Increment(ref D),
            () => Decrement(ref D),
            () => Load(ref D, Fetch()),
            () => RotateLeft(ref A, false),
            () => JumpBy(Fetch()),
            () => Add(ref H, ref L, D, E),
            () => Load(ref A, Read(DE)),
            () => Decrement(ref D, ref E),
            () => Increment(ref E),
            () => Decrement(ref E),
            () => Load(ref E, Fetch()),
            () => RotateRight(ref A, false),



            // 0x2X
            () => ConditionalJumpBy(!ZeroFlag, Fetch()),
            () => Load(ref H, ref L, GetDirectAddress()),
            () => { LoadToMem(HL, A); Increment(ref H, ref L); },
            () => Increment(ref H, ref L),
            () => Increment(ref H),
            () => Decrement(ref H),
            () => Load(ref H, Fetch()),
            DAA,
            () => ConditionalJumpBy(ZeroFlag, Fetch()),
            () => Add(ref H, ref L, H, L),
            () => { Load(ref A, Read(HL)); Increment(ref H, ref L); },
            () => Decrement(ref H, ref L),
            () => Increment(ref L),
            () => Decrement(ref L),
            () => Load(ref L, Fetch()),
            () => Complement(ref A),



            // 0x3X
            () => ConditionalJumpBy(!CarryFlag, Fetch()),
            () => Load(ref SP, GetDirectAddress()),
            () => { LoadToMem(HL, A); Decrement(ref H, ref L); },
            () => Increment(ref SP),
            () => IncrementInMemory(H, L),
            () => DecrementInMemory(H, L),
            () => LoadToMem(HL, Fetch()),
            SetCarryFlagInstruction,
            () => ConditionalJumpBy(CarryFlag, Fetch()),
            () => Add(ref H, ref L, SP_S, SP_P),
            () => { Load(ref A, Read(HL)); Decrement(ref H, ref L); },
            () => Decrement(ref SP),
            () => Increment(ref A),
            () => Decrement(ref A),
            () => Load(ref A, Fetch()),
            ComplementCarryFlag,



            // 0x4X
            () => Load(ref B, B),
            () => Load(ref B, C),
            () => Load(ref B, D),
            () => Load(ref B, E),
            () => Load(ref B, H),
            () => Load(ref B, L),
            () => Load(ref B, Read(HL)),
            () => Load(ref B, A),
            () => Load(ref C, B),
            () => Load(ref C, C),
            () => Load(ref C, D),
            () => Load(ref C, E),
            () => Load(ref C, H),
            () => Load(ref C, L),
            () => Load(ref C, Read(HL)),
            () => Load(ref C, A),



            // 0x5X
            () => Load(ref D, B),
            () => Load(ref D, C),
            () => Load(ref D, D),
            () => Load(ref D, E),
            () => Load(ref D, H),
            () => Load(ref D, L),
            () => Load(ref D, Read(HL)),
            () => Load(ref D, A),
            () => Load(ref E, B),
            () => Load(ref E, C),
            () => Load(ref E, D),
            () => Load(ref E, E),
            () => Load(ref E, H),
            () => Load(ref E, L),
            () => Load(ref E, Read(HL)),
            () => Load(ref E, A),



            // 0x6X
            () => Load(ref H, B),
            () => Load(ref H, C),
            () => Load(ref H, D),
            () => Load(ref H, E),
            () => Load(ref H, H),
            () => Load(ref H, L),
            () => Load(ref H, Read(HL)),
            () => Load(ref H, A),
            () => Load(ref L, B),
            () => Load(ref L, C),
            () => Load(ref L, D),
            () => Load(ref L, E),
            () => Load(ref L, H),
            () => Load(ref L, L),
            () => Load(ref L, Read(HL)),
            () => Load(ref L, A),



            // 0x7X
            () => LoadToMem(HL, B),
            () => LoadToMem(HL, C),
            () => LoadToMem(HL, D),
            () => LoadToMem(HL, E),
            () => LoadToMem(HL, H),
            () => LoadToMem(HL, L),
            Halt,
            () => LoadToMem(HL, A),
            () => Load(ref A, B),
            () => Load(ref A, C),
            () => Load(ref A, D),
            () => Load(ref A, E),
            () => Load(ref A, H),
            () => Load(ref A, L),
            () => Load(ref A, Read(HL)),
            () => Load(ref A, A),



            // 0x8X
            () => Add(ref A, B),
            () => Add(ref A, C),
            () => Add(ref A, D),
            () => Add(ref A, E),
            () => Add(ref A, H),
            () => Add(ref A, L),
            () => Add(ref A, Read(HL)),
            () => Add(ref A, A),
            () => Add(ref A, B, CarryFlag),
            () => Add(ref A, C, CarryFlag),
            () => Add(ref A, D, CarryFlag),
            () => Add(ref A, E, CarryFlag),
            () => Add(ref A, H, CarryFlag),
            () => Add(ref A, L, CarryFlag),
            () => Add(ref A, Read(HL), CarryFlag),
            () => Add(ref A, A, CarryFlag),



            // 0x9X
            () => Subtract(ref A, B),
            () => Subtract(ref A, C),
            () => Subtract(ref A, D),
            () => Subtract(ref A, E),
            () => Subtract(ref A, H),
            () => Subtract(ref A, L),
            () => Subtract(ref A, Read(HL)),
            () => Subtract(ref A, A),
            () => Subtract(ref A, B, CarryFlag),
            () => Subtract(ref A, C, CarryFlag),
            () => Subtract(ref A, D, CarryFlag),
            () => Subtract(ref A, E, CarryFlag),
            () => Subtract(ref A, H, CarryFlag),
            () => Subtract(ref A, L, CarryFlag),
            () => Subtract(ref A, Read(HL), CarryFlag),
            () => Subtract(ref A, A, CarryFlag),



            // 0xAX
            () => And(ref A, B),
            () => And(ref A, C),
            () => And(ref A, D),
            () => And(ref A, E),
            () => And(ref A, H),
            () => And(ref A, L),
            () => And(ref A, Read(HL)),
            () => And(ref A, A),
            () => Xor(ref A, B),
            () => Xor(ref A, C),
            () => Xor(ref A, D),
            () => Xor(ref A, E),
            () => Xor(ref A, H),
            () => Xor(ref A, L),
            () => Xor(ref A, Read(HL)),
            () => Xor(ref A, A),



            // 0xBX
            () => Or(ref A, B),
            () => Or(ref A, C),
            () => Or(ref A, D),
            () => Or(ref A, E),
            () => Or(ref A, H),
            () => Or(ref A, L),
            () => Or(ref A, Read(HL)),
            () => Or(ref A, A),
            () => Compare(A, B),
            () => Compare(A, C),
            () => Compare(A, D),
            () => Compare(A, E),
            () => Compare(A, H),
            () => Compare(A, L),
            () => Compare(A, Read(HL)),
            () => Compare(A, A),



            // 0xCX
            () => ConditionalReturn(!ZeroFlag),
            () => Pop(ref B, ref C),
            () => ConditionalJumpTo(!ZeroFlag, GetDirectAddress()),
            () => JumpTo(GetDirectAddress()),
            () => ConditionalCall(!ZeroFlag, GetDirectAddress()),
            () => Push(B, C),
            () => Add(ref A, Fetch()),
            () => Restart(0x00),
            () => ConditionalReturn(ZeroFlag),
            Return,
            () => ConditionalJumpTo(ZeroFlag, GetDirectAddress()),
            Prefix_CB,
            () => ConditionalCall(ZeroFlag, GetDirectAddress()),
            () => Call(GetDirectAddress()),
            () => Add(ref A, Fetch(), CarryFlag),
            () => Restart(0x08),



            // 0xDX
            () => ConditionalReturn(!CarryFlag),
            () => Pop(ref D, ref E),
            () => ConditionalJumpTo(!CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(!CarryFlag, GetDirectAddress()),
            () => Push(D, E),
            () => Subtract(ref A, Fetch()),
            () => Restart(0x10),
            () => ConditionalReturn(CarryFlag),
            ReturnAndEnableInterrupt,
            () => ConditionalJumpTo(CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(CarryFlag, GetDirectAddress()),
            Empty,
            () => Subtract(ref A, Fetch(), CarryFlag),
            () => Restart(0x18),



            // 0xEX
            () => LoadToMem((ushort)(0xFF00 + Fetch()), A),
            () => Pop(ref H, ref L),
            () => LoadToMem((ushort)(0xFF00 + C), A),
            Empty,
            Empty,
            () => Push(H, L),
            () => And(ref A, Fetch()),
            () => Restart(0x20),
            () => AddToStackPointer(Fetch()),
            () => JumpTo(HL),
            () => LoadToMem(GetDirectAddress(), A),
            Empty,
            Empty,
            Empty,
            () => Xor(ref A, Fetch()),
            () => Restart(0x28),



            // 0xFX
            () => Load(ref A, Read((ushort)(0xFF00 + Fetch()))),
            () => Pop(ref A, ref F, true),
            () => Load(ref A, Read((ushort)(0xFF00 + C))),
            DisableInterrupt,
            Empty,
            () => Push(A, F),
            () => Or(ref A, Fetch()),
            () => Restart(0x30),
            () => { ushort prevSP = SP; AddToStackPointer(Fetch()); Load(ref H, ref L, SP); SP = prevSP; },
            () => Load(ref SP, HL),
            () => Load(ref A, Read(GetDirectAddress())),
            EnableInterrupt,
            Empty,
            Empty,
            () => Compare(A, Fetch()),
            () => Restart(0x38)
            };

            durations = new byte[0x100]{
             4,12, 8, 8, 4, 4, 8, 4,  20, 8, 8, 8, 4, 4, 8, 4,
             4,12, 8, 8, 4, 4, 8, 4,  12, 8, 8, 8, 4, 4, 8, 4,
             8,12, 8, 8, 4, 4, 8, 4,   8, 8, 8, 8, 4, 4, 8, 4,
             8,12, 8, 8,12,12,12, 4,   8, 8, 8, 8, 4, 4, 8, 4,

             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 4, 4,   4, 4, 4, 4, 4, 4, 8, 4,

             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,
             4, 4, 4, 4, 4, 4, 8, 4,   4, 4, 4, 4, 4, 4, 8, 4,

             8,12,12,16,12,16, 8,16,   8,16,12, 4,12,24, 8,16,
             8,12,12, 0,12,16, 8,16,   8,16,12, 0,12, 0, 8,16,
            12,12, 8, 0, 0,16, 8,16,  16, 4,16, 0, 0, 0, 8,16,
            12,12, 8, 4, 0,16, 8,16,  12, 8,16, 4, 0, 0, 8,16,
            };

            //setup cb instructions
            cbInstructions = new Action[0x100]
            {
            // 0x0X
            () => RotateLeftWithCarry(ref B),
            () => RotateLeftWithCarry(ref C),
            () => RotateLeftWithCarry(ref D),
            () => RotateLeftWithCarry(ref E),
            () => RotateLeftWithCarry(ref H),
            () => RotateLeftWithCarry(ref L),
            () => RotateLeftWithCarry(HL),
            () => RotateLeftWithCarry(ref A),
            () => RotateRightWithCarry(ref B),
            () => RotateRightWithCarry(ref C),
            () => RotateRightWithCarry(ref D),
            () => RotateRightWithCarry(ref E),
            () => RotateRightWithCarry(ref H),
            () => RotateRightWithCarry(ref L),
            () => RotateRightWithCarry(HL),
            () => RotateRightWithCarry(ref A),



            // 0x1X
            () => RotateLeft(ref B),
            () => RotateLeft(ref C),
            () => RotateLeft(ref D),
            () => RotateLeft(ref E),
            () => RotateLeft(ref H),
            () => RotateLeft(ref L),
            () => RotateLeft(HL),
            () => RotateLeft(ref A),
            () => RotateRight(ref B),
            () => RotateRight(ref C),
            () => RotateRight(ref D),
            () => RotateRight(ref E),
            () => RotateRight(ref H),
            () => RotateRight(ref L),
            () => RotateRight(HL),
            () => RotateRight(ref A),



            // 0x2X
            () => ShiftLeftA(ref B),
            () => ShiftLeftA(ref C),
            () => ShiftLeftA(ref D),
            () => ShiftLeftA(ref E),
            () => ShiftLeftA(ref H),
            () => ShiftLeftA(ref L),
            () => ShiftLeftA(HL),
            () => ShiftLeftA(ref A),
            () => ShiftRightA(ref B),
            () => ShiftRightA(ref C),
            () => ShiftRightA(ref D),
            () => ShiftRightA(ref E),
            () => ShiftRightA(ref H),
            () => ShiftRightA(ref L),
            () => ShiftRightA(HL),
            () => ShiftRightA(ref A),


            
            // 0x3X
            () => Swap(ref B),
            () => Swap(ref C),
            () => Swap(ref D),
            () => Swap(ref E),
            () => Swap(ref H),
            () => Swap(ref L),
            () => Swap(HL),
            () => Swap(ref A),
            () => ShiftRightL(ref B),
            () => ShiftRightL(ref C),
            () => ShiftRightL(ref D),
            () => ShiftRightL(ref E),
            () => ShiftRightL(ref H),
            () => ShiftRightL(ref L),
            () => ShiftRightL(HL),
            () => ShiftRightL(ref A),


            
            // 0x4X
            () => Bit(0, B),
            () => Bit(0, C),
            () => Bit(0, D),
            () => Bit(0, E),
            () => Bit(0, H),
            () => Bit(0, L),
            () => Bit(0, Read(HL)),
            () => Bit(0, A),
            () => Bit(1, B),
            () => Bit(1, C),
            () => Bit(1, D),
            () => Bit(1, E),
            () => Bit(1, H),
            () => Bit(1, L),
            () => Bit(1, Read(HL)),
            () => Bit(1, A),



            // 0x5X
            () => Bit(2, B),
            () => Bit(2, C),
            () => Bit(2, D),
            () => Bit(2, E),
            () => Bit(2, H),
            () => Bit(2, L),
            () => Bit(2, Read(HL)),
            () => Bit(2, A),
            () => Bit(3, B),
            () => Bit(3, C),
            () => Bit(3, D),
            () => Bit(3, E),
            () => Bit(3, H),
            () => Bit(3, L),
            () => Bit(3, Read(HL)),
            () => Bit(3, A),



            // 0x6X
            () => Bit(4, B),
            () => Bit(4, C),
            () => Bit(4, D),
            () => Bit(4, E),
            () => Bit(4, H),
            () => Bit(4, L),
            () => Bit(4, Read(HL)),
            () => Bit(4, A),
            () => Bit(5, B),
            () => Bit(5, C),
            () => Bit(5, D),
            () => Bit(5, E),
            () => Bit(5, H),
            () => Bit(5, L),
            () => Bit(5, Read(HL)),
            () => Bit(5, A),



            // 0x7X
            () => Bit(6, B),
            () => Bit(6, C),
            () => Bit(6, D),
            () => Bit(6, E),
            () => Bit(6, H),
            () => Bit(6, L),
            () => Bit(6, Read(HL)),
            () => Bit(6, A),
            () => Bit(7, B),
            () => Bit(7, C),
            () => Bit(7, D),
            () => Bit(7, E),
            () => Bit(7, H),
            () => Bit(7, L),
            () => Bit(7, Read(HL)),
            () => Bit(7, A),



            // 0x8X
            () => Reset(0, ref B),
            () => Reset(0, ref C),
            () => Reset(0, ref D),
            () => Reset(0, ref E),
            () => Reset(0, ref H),
            () => Reset(0, ref L),
            () => Reset(0, HL),
            () => Reset(0, ref A),
            () => Reset(1, ref B),
            () => Reset(1, ref C),
            () => Reset(1, ref D),
            () => Reset(1, ref E),
            () => Reset(1, ref H),
            () => Reset(1, ref L),
            () => Reset(1, HL),
            () => Reset(1, ref A),



            // 0x9X
            () => Reset(2, ref B),
            () => Reset(2, ref C),
            () => Reset(2, ref D),
            () => Reset(2, ref E),
            () => Reset(2, ref H),
            () => Reset(2, ref L),
            () => Reset(2, HL),
            () => Reset(2, ref A),
            () => Reset(3, ref B),
            () => Reset(3, ref C),
            () => Reset(3, ref D),
            () => Reset(3, ref E),
            () => Reset(3, ref H),
            () => Reset(3, ref L),
            () => Reset(3, HL),
            () => Reset(3, ref A),



            // 0xAX
            () => Reset(4, ref B),
            () => Reset(4, ref C),
            () => Reset(4, ref D),
            () => Reset(4, ref E),
            () => Reset(4, ref H),
            () => Reset(4, ref L),
            () => Reset(4, HL),
            () => Reset(4, ref A),
            () => Reset(5, ref B),
            () => Reset(5, ref C),
            () => Reset(5, ref D),
            () => Reset(5, ref E),
            () => Reset(5, ref H),
            () => Reset(5, ref L),
            () => Reset(5, HL),
            () => Reset(5, ref A),



            // 0xBX
            () => Reset(6, ref B),
            () => Reset(6, ref C),
            () => Reset(6, ref D),
            () => Reset(6, ref E),
            () => Reset(6, ref H),
            () => Reset(6, ref L),
            () => Reset(6, HL),
            () => Reset(6, ref A),
            () => Reset(7, ref B),
            () => Reset(7, ref C),
            () => Reset(7, ref D),
            () => Reset(7, ref E),
            () => Reset(7, ref H),
            () => Reset(7, ref L),
            () => Reset(7, HL),
            () => Reset(7, ref A),



            // 0xCX
            () => Set(0, ref B),
            () => Set(0, ref C),
            () => Set(0, ref D),
            () => Set(0, ref E),
            () => Set(0, ref H),
            () => Set(0, ref L),
            () => Set(0, HL),
            () => Set(0, ref A),
            () => Set(1, ref B),
            () => Set(1, ref C),
            () => Set(1, ref D),
            () => Set(1, ref E),
            () => Set(1, ref H),
            () => Set(1, ref L),
            () => Set(1, HL),
            () => Set(1, ref A),



            // 0xDX
            () => Set(2, ref B),
            () => Set(2, ref C),
            () => Set(2, ref D),
            () => Set(2, ref E),
            () => Set(2, ref H),
            () => Set(2, ref L),
            () => Set(2, HL),
            () => Set(2, ref A),
            () => Set(3, ref B),
            () => Set(3, ref C),
            () => Set(3, ref D),
            () => Set(3, ref E),
            () => Set(3, ref H),
            () => Set(3, ref L),
            () => Set(3, HL),
            () => Set(3, ref A),



            // 0xEX
            () => Set(4, ref B),
            () => Set(4, ref C),
            () => Set(4, ref D),
            () => Set(4, ref E),
            () => Set(4, ref H),
            () => Set(4, ref L),
            () => Set(4, HL),
            () => Set(4, ref A),
            () => Set(5, ref B),
            () => Set(5, ref C),
            () => Set(5, ref D),
            () => Set(5, ref E),
            () => Set(5, ref H),
            () => Set(5, ref L),
            () => Set(5, HL),
            () => Set(5, ref A),



            // 0xFX
            () => Set(6, ref B),
            () => Set(6, ref C),
            () => Set(6, ref D),
            () => Set(6, ref E),
            () => Set(6, ref H),
            () => Set(6, ref L),
            () => Set(6, HL),
            () => Set(6, ref A),
            () => Set(7, ref B),
            () => Set(7, ref C),
            () => Set(7, ref D),
            () => Set(7, ref E),
            () => Set(7, ref H),
            () => Set(7, ref L),
            () => Set(7, HL),
            () => Set(7, ref A),
            };
        }

        // GB-docs source: http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
        // Instructions matrix: https://pastraiser.com/cpu/gameboy/gameboy_opcodes.html 

    }
}