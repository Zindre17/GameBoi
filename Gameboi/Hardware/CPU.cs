using System;
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

    public class CPU : Hardware, ILoop
    {
        private LogFileWriter logger;
        private readonly SpeedMode speedMode = new();
        private ulong Speed => speedMode.Mode;

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

        internal ulong GetSpeed()
        {
            return speedMode.Mode;
        }

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
            speedMode.Reset();
            SetFramerate(normalFramerate);
            IME = true;
            shouldUpdateIME = false;
            nextIMEValue = false;
            IF.Write(0);
            IE.Write(0);
            logger = new LogFileWriter();
        }

        private const float normalFramerate = 60;
        private const uint cyclesPerFrame = (uint)(cpuSpeed / normalFramerate);


        private float currentFramerate;
        public float MillisecondsPerLoop { get; set; }

        private void SetFramerate(float framerate)
        {
            currentFramerate = framerate;
            MillisecondsPerLoop = 1000 / framerate;
        }

        public void ChangeSpeed(bool faster)
        {
            if (faster)
            {
                SetFramerate(Math.Min(currentFramerate * 2, normalFramerate * 8));
            }
            else
            {
                SetFramerate(Math.Max(currentFramerate / 2, normalFramerate / 4));
            }
        }

        private ulong elapsed = 0;

        public void ExecuteInstructionsBulk(long _)
        {
            ulong start = Cycles;
            while (elapsed < cyclesPerFrame * speedMode.Mode)
            {
                DoNextInstruction();
                elapsed = Cycles - start;
            }
            elapsed -= cyclesPerFrame;
        }

        public Action<long> Loop => ExecuteInstructionsBulk;

        private bool shouldLog = false;
        private string logLine;
        public void DoNextInstruction()
        {
            logLine = GetInternalStateString();

            if (isHalted)
            {
                logLine += " Is Halted; ";
                bus.UpdateCycles(4, Speed);
                NoOperation();
            }
            else
            {
                // Fetch, Decode, Execute
                byte opCode = Fetch();
                bus.UpdateCycles(durations[opCode], Speed);
                instructions[opCode]();
            }

            HandleInterrupts();

            if (shouldLog)
            {
                logger.LogLine(logLine);
            }
        }

        private const string internalStateFormat = "A:0x{0:X2} | F:0x{1:X2} | BC:0x{2:X4} | DE:0x{3:X4} | HL:0x{4:X4} | SP:0x{5:X4} | PC:0x{6:X4} | IME:{7} | IE:0x{8:X2} | IF:0x{9:X2} |";
        private string GetInternalStateString()
        {
            return string.Format(internalStateFormat, A, F, BC, DE, HL, SP, PC, IME, IE.Read(), IF.Read());
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
            if ((IF.Read() & IE.Read()) == 0xE0) return;

            Interrupt();
        }

        // type             :   prio    : address   : bit
        //---------------------------------------------------
        // V-Blank          :     1     : 0x0040    : 0
        // LCDC Status      :     2     : 0x0048    : 1
        // Timer Overflow   :     3     : 0x0050    : 2
        // Serial Transfer  :     4     : 0x0058    : 3
        // Hi-Lo of P10-P13 :     5     : 0x0060    : 4
        private void Interrupt()
        {
            var tempIF = new InterruptRegister();
            var tempIE = new InterruptRegister();
            IME = false;
            Write(--SP, PC_P);
            // Push of high byte to stack can cancel interrupts
            // But its too late when pushing the low byte
            tempIF.Write(IF.Read());
            tempIE.Write(IE.Read());
            Write(--SP, PC_C);
            ushort interruptVector = 0;
            if (tempIE.Vblank && tempIF.Vblank)
            {
                IF.Vblank = false;
                interruptVector = VblankVector;
            }
            else if (tempIE.LcdStat && tempIF.LcdStat)
            {
                IF.LcdStat = false;
                interruptVector = LcdStatVector;
            }
            else if (tempIE.Timer && tempIF.Timer)
            {
                IF.Timer = false;
                interruptVector = TimerVector;
            }
            else if (tempIE.Serial && tempIF.Serial)
            {
                IF.Serial = false;
                interruptVector = SerialVector;
            }
            else if (tempIE.Joypad && tempIF.Joypad)
            {
                IF.Joypad = false;
                interruptVector = JoypadVector;
            }
            JumpTo(interruptVector);
            bus.UpdateCycles(20, Speed);
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
            bus.ReplaceMemory(0xFF4D, speedMode);
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
        private void Empty()
        {
            logLine += " Empty Instruction;";
        }
        private void NoOperation()
        {
            logLine += " No operation;";
        }
        private void Stop(byte _)
        {
            logLine += " Stop;";
            if (speedMode.ShouldSwapSpeed)
            {
                logLine += $" Swapping CPU speed from {speedMode.Mode} to {(speedMode.Mode == SpeedMode.DoubleSpeed ? SpeedMode.NormalSpeed : SpeedMode.DoubleSpeed)};";
                speedMode.SwapSpeed();
            }
            //TODO: display white line in center and do nothing untill any button is pressed. 
        }
        private void Halt()
        {
            // Halts CPU until interrupt happens => Perform NOPs meanwhile to not fuck up memory
            logLine += " Halting;";
            isHalted = true;
        }

        private void DisableInterrupt()
        {
            logLine += " Disabling interrupts;";
            if (IME)
            {
                shouldUpdateIME = true;
                nextIMEValue = false;
            }
        }

        private void EnableInterrupt()
        {
            logLine += " Enabling interrupts;";
            if (!IME)
            {
                shouldUpdateIME = true;
                nextIMEValue = true;
            }
        }

        private void Prefix_CB()
        {
            logLine += " CB-instruction;";
            byte opCode = Fetch();
            cbInstructions[opCode]();
            Byte modded = opCode % 8;
            Byte duration = modded == 6 ? 16 : 8;
            bus.UpdateCycles(duration, Speed);
        }

        private void SetCarryFlagInstruction()
        {
            logLine += " Setting carry flag;";
            SetCarryFlag(true);
            SetHalfCarryFlag(false);
            SetSubtractFlag(false);
        }
        private void ComplementCarryFlag()
        {
            logLine += " Flipping carry flag;";
            SetSubtractFlag(false);
            SetHalfCarryFlag(false);
            SetCarryFlag(!CarryFlag);
        }
        #endregion

        #region Loads
        private const string loadLogFormat1 = " Loading 0x{0:X2} into {1};";
        private const string loadLogFormat2 = " Loading 0x{0:X4} into {1};";
        private void Load(ref byte target, byte source, string label = "")
        {
            logLine += string.Format(loadLogFormat1, source, label);
            target = source;
        }
        private void Load(ref byte targetHigh, ref byte targetLow, ushort value, string label = "")
        {
            logLine += string.Format(loadLogFormat2, value, label);
            targetLow = GetLowByte(value);
            targetHigh = GetHighByte(value);
        }
        private void Load(ref ushort target, ushort value, string label = "")
        {
            logLine += string.Format(loadLogFormat2, value, label);
            target = value;
        }

        private const string loadToMemFormat = " Loading 0x{0:X2} into {1:X4};";
        private void LoadToMem(ushort address, byte source)
        {
            logLine += string.Format(loadToMemFormat, source, address);
            Write(address, source);
        }
        private void LoadToMem(ushort address, ushort source)
        {
            var lowByte = GetLowByte(source);
            var highByte = GetHighByte(source);
            logLine += string.Format(loadToMemFormat, lowByte, address);
            logLine += string.Format(loadToMemFormat, highByte, address + 1);
            Write(address, lowByte);
            Write((ushort)(address + 1), highByte);
        }
        #endregion

        #region Aritmetic
        private void DAA()
        {
            logLine += " DAA;";

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

        private const string add8Format = " Add A and {0} into A;";
        private void Add(ref byte target, byte operand, bool withCarry = false, string operandLabel = "")
        {
            logLine += string.Format(add8Format, operandLabel);
            target = Add8(target, operand, out bool C, out bool H, withCarry);
            SetFlags(target == 0, false, H, C);
        }

        private const string add16Format = " Add HL and {0} into HL;";
        private void Add(ref byte targetHigh, ref byte targetLow, byte operandHigh, byte operandLow, string operandLabel = "")
        {
            logLine += string.Format(add16Format, operandLabel);
            ushort target = ConcatBytes(targetHigh, targetLow);
            ushort operand = ConcatBytes(operandHigh, operandLow);
            ushort result = Add16(target, operand, out bool C, out bool H);
            targetHigh = GetHighByte(result);
            targetLow = GetLowByte(result);
            SetSubtractFlag(false);
            SetCarryFlag(C);
            SetHalfCarryFlag(H);
        }

        private const string subtractFormat = " Subtract {0} from A into A;";
        private void Subtract(ref byte target, byte operand, bool withCarry = false, string operandLabel = "")
        {
            logLine += string.Format(subtractFormat, operandLabel);
            target = Sub8(target, operand, out bool C, out bool H, withCarry);
            SetFlags(target == 0, true, H, C);
        }

        private const string incrementFormat = " Incrementing {0};";
        private void Increment(ref byte target, string targetLabel = "")
        {
            logLine += string.Format(incrementFormat, targetLabel);
            Byte low4 = target & 0xF;
            SetHalfCarryFlag(low4 == 0xF); // set if carry from bit 3
            target++;
            SetZeroFlag(target == 0);
            SetSubtractFlag(false);
        }
        private void Increment(ref byte targetHigh, ref byte targetLow, string targetLabel = "")
        {
            logLine += string.Format(incrementFormat, targetLabel);
            int newLowByte = targetLow + 1;
            if (newLowByte > 0xFF)
            {
                targetHigh++;
            }
            targetLow = (byte)newLowByte;
        }
        private void Increment(ref ushort target, string targetLabel = "")
        {
            logLine += string.Format(incrementFormat, targetLabel);
            target++;
        }
        private void IncrementInMemory(byte addressHigh, byte addressLow)
        {
            ushort address = ConcatBytes(addressHigh, addressLow);
            byte value = Read(address);
            Increment(ref value, string.Format("(0x{0:X4})", address));
            Write(address, value);
        }

        private const string decrementFormat = " Decrementing {0};";
        private void Decrement(ref byte target, string targetLabel = "")
        {
            logLine += string.Format(decrementFormat, targetLabel);
            Byte low4 = target & 0xF;
            SetHalfCarryFlag(low4 == 0); // set if borrow from bit 4
            target--;
            SetZeroFlag(target == 0);
            SetSubtractFlag(true);
        }
        private void Decrement(ref byte targetHigh, ref byte targetLow, string targetLabel = "")
        {
            logLine += string.Format(decrementFormat, targetLabel);
            int newLowByte = targetLow - 1;
            if (newLowByte < 0)
            {
                targetHigh--;
            }
            targetLow = (byte)newLowByte;
        }
        private void Decrement(ref ushort target, string targetLabel = "")
        {
            logLine += string.Format(decrementFormat, targetLabel);
            target--;
        }
        private void DecrementInMemory(byte addresshigh, byte addressLow)
        {
            ushort address = ConcatBytes(addresshigh, addressLow);
            byte value = Read(address);
            Decrement(ref value, string.Format("(0x{0:X4})", address));
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

        private const string conditionalJumpFormat = " Conditional jump: {0};";
        private void ConditionalJumpBy(bool condition, byte increment)
        {
            logLine += string.Format(conditionalJumpFormat, condition);
            if (condition)
            {
                bus.UpdateCycles(4, Speed);
                JumpBy(increment);
            }
        }
        private void JumpBy(byte increment) //actually signed
        {
            logLine += string.Format(" Jumping by {0};", (sbyte)increment);
            PC = (ushort)(PC + (sbyte)increment);
        }

        private void ConditionalJumpTo(bool condition, ushort address)
        {
            logLine += string.Format(conditionalJumpFormat, condition);
            if (condition)
            {
                bus.UpdateCycles(4, Speed);
                JumpTo(address);
            }
        }
        private void JumpTo(ushort newPC)
        {
            logLine += string.Format(" Jumping to 0x{0:X4};", newPC);
            PC = newPC;
        }
        private void ConditionalReturn(bool condition)
        {
            logLine += string.Format(" Conditional return: {0};", condition);
            if (condition)
            {
                bus.UpdateCycles(12, Speed);
                Return();
            }
        }
        private void Return()
        {
            logLine += " Returning;";
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
            logLine += string.Format(" Conditional call: {0};", condition);
            if (condition)
            {
                bus.UpdateCycles(12, Speed);
                Call(address);
            }
        }
        private void Call(ushort address)
        {
            logLine += string.Format(" Calling 0x{0:X4};", address);
            Push(PC_P, PC_C, nameof(PC));
            JumpTo(address);
        }
        private void Restart(byte newPC)
        {
            logLine += string.Format(" Restarting at 0x{0:X4};", newPC);
            Push(PC_P, PC_C, nameof(PC));
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
        private void Push(byte high, byte low, string label = "")
        {
            logLine += string.Format(" Pushing {0} to Stack;", label);
            Write(--SP, high);
            Write(--SP, low);
        }
        private void Pop(ref byte targetHigh, ref byte targetLow, bool isFlagRegister = false, string targetLabel = "")
        {
            logLine += string.Format(" Popping from Stack into {0};", targetLabel);
            targetLow = Read(SP++);
            if (isFlagRegister)
                targetLow &= 0xF0;
            targetHigh = Read(SP++);
        }
        private void AddToStackPointer(byte operand)
        {
            logLine += string.Format(" Adding {0} to SP;", (sbyte)operand);
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
            () => Load(ref B, ref C, GetDirectAddress(), nameof(BC)),
            () => LoadToMem(BC, A),
            () => Increment(ref B, ref C, nameof(BC)),
            () => Increment(ref B, nameof(B)),
            () => Decrement(ref B, nameof(B)),
            () => Load(ref B, Fetch(), nameof(B)),
            () => RotateLeftWithCarry(ref A, false),
            () => LoadToMem(GetDirectAddress(),SP),
            () => Add(ref H, ref L, B, C, nameof(BC)),
            () => Load(ref A, Read(BC), nameof(A)),
            () => Decrement(ref B, ref C, nameof(BC)),
            () => Increment(ref C, nameof(C)),
            () => Decrement(ref C, nameof(C)),
            () => Load(ref C, Fetch(), nameof(C)),
            () => RotateRightWithCarry(ref A, false),



            // 0x1X
            () => Stop(Fetch()),
            () => Load(ref D, ref E, GetDirectAddress(), nameof(DE)),
            () => LoadToMem(DE, A),
            () => Increment(ref D, ref E, nameof(DE)),
            () => Increment(ref D, nameof(D)),
            () => Decrement(ref D, nameof(D)),
            () => Load(ref D, Fetch(), nameof(D)),
            () => RotateLeft(ref A, false),
            () => JumpBy(Fetch()),
            () => Add(ref H, ref L, D, E, nameof(DE)),
            () => Load(ref A, Read(DE), nameof(A)),
            () => Decrement(ref D, ref E, nameof(DE)),
            () => Increment(ref E, nameof(E)),
            () => Decrement(ref E, nameof(E)),
            () => Load(ref E, Fetch(), nameof(E)),
            () => RotateRight(ref A, false),



            // 0x2X
            () => ConditionalJumpBy(!ZeroFlag, Fetch()),
            () => Load(ref H, ref L, GetDirectAddress(), nameof(HL)),
            () => { LoadToMem(HL, A); Increment(ref H, ref L, nameof(HL)); },
            () => Increment(ref H, ref L, nameof(HL)),
            () => Increment(ref H, nameof(H)),
            () => Decrement(ref H, nameof(H)),
            () => Load(ref H, Fetch(), nameof(H)),
            DAA,
            () => ConditionalJumpBy(ZeroFlag, Fetch()),
            () => Add(ref H, ref L, H, L, nameof(HL)),
            () => { Load(ref A, Read(HL), nameof(A)); Increment(ref H, ref L, nameof(HL)); },
            () => Decrement(ref H, ref L, nameof(HL)),
            () => Increment(ref L, nameof(L)),
            () => Decrement(ref L, nameof(L)),
            () => Load(ref L, Fetch(), nameof(L)),
            () => Complement(ref A),



            // 0x3X
            () => ConditionalJumpBy(!CarryFlag, Fetch()),
            () => Load(ref SP, GetDirectAddress(), nameof(SP)),
            () => { LoadToMem(HL, A); Decrement(ref H, ref L, nameof(HL)); },
            () => Increment(ref SP, nameof(SP)),
            () => IncrementInMemory(H, L),
            () => DecrementInMemory(H, L),
            () => LoadToMem(HL, Fetch()),
            SetCarryFlagInstruction,
            () => ConditionalJumpBy(CarryFlag, Fetch()),
            () => Add(ref H, ref L, SP_S, SP_P, nameof(SP)),
            () => { Load(ref A, Read(HL), nameof(A)); Decrement(ref H, ref L, nameof(HL)); },
            () => Decrement(ref SP, nameof(SP)),
            () => Increment(ref A, nameof(A)),
            () => Decrement(ref A, nameof(A)),
            () => Load(ref A, Fetch(), nameof(A)),
            ComplementCarryFlag,



            // 0x4X
            () => Load(ref B, B, nameof(B)),
            () => Load(ref B, C, nameof(B)),
            () => Load(ref B, D, nameof(B)),
            () => Load(ref B, E, nameof(B)),
            () => Load(ref B, H, nameof(B)),
            () => Load(ref B, L, nameof(B)),
            () => Load(ref B, Read(HL), nameof(B)),
            () => Load(ref B, A, nameof(B)),
            () => Load(ref C, B, nameof(C)),
            () => Load(ref C, C, nameof(C)),
            () => Load(ref C, D, nameof(C)),
            () => Load(ref C, E, nameof(C)),
            () => Load(ref C, H, nameof(C)),
            () => Load(ref C, L, nameof(C)),
            () => Load(ref C, Read(HL), nameof(C)),
            () => Load(ref C, A, nameof(C)),



            // 0x5X
            () => Load(ref D, B, nameof(D)),
            () => Load(ref D, C, nameof(D)),
            () => Load(ref D, D, nameof(D)),
            () => Load(ref D, E, nameof(D)),
            () => Load(ref D, H, nameof(D)),
            () => Load(ref D, L, nameof(D)),
            () => Load(ref D, Read(HL), nameof(D)),
            () => Load(ref D, A, nameof(D)),
            () => Load(ref E, B, nameof(E)),
            () => Load(ref E, C, nameof(E)),
            () => Load(ref E, D, nameof(E)),
            () => Load(ref E, E, nameof(E)),
            () => Load(ref E, H, nameof(E)),
            () => Load(ref E, L, nameof(E)),
            () => Load(ref E, Read(HL), nameof(E)),
            () => Load(ref E, A, nameof(E)),



            // 0x6X
            () => Load(ref H, B, nameof(H)),
            () => Load(ref H, C, nameof(H)),
            () => Load(ref H, D, nameof(H)),
            () => Load(ref H, E, nameof(H)),
            () => Load(ref H, H, nameof(H)),
            () => Load(ref H, L, nameof(H)),
            () => Load(ref H, Read(HL), nameof(H)),
            () => Load(ref H, A, nameof(H)),
            () => Load(ref L, B, nameof(L)),
            () => Load(ref L, C, nameof(L)),
            () => Load(ref L, D, nameof(L)),
            () => Load(ref L, E, nameof(L)),
            () => Load(ref L, H, nameof(L)),
            () => Load(ref L, L, nameof(L)),
            () => Load(ref L, Read(HL), nameof(L)),
            () => Load(ref L, A, nameof(L)),



            // 0x7X
            () => LoadToMem(HL, B),
            () => LoadToMem(HL, C),
            () => LoadToMem(HL, D),
            () => LoadToMem(HL, E),
            () => LoadToMem(HL, H),
            () => LoadToMem(HL, L),
            Halt,
            () => LoadToMem(HL, A),
            () => Load(ref A, B, nameof(A)),
            () => Load(ref A, C, nameof(A)),
            () => Load(ref A, D, nameof(A)),
            () => Load(ref A, E, nameof(A)),
            () => Load(ref A, H, nameof(A)),
            () => Load(ref A, L, nameof(A)),
            () => Load(ref A, Read(HL), nameof(A)),
            () => Load(ref A, A, nameof(A)),



            // 0x8X
            () => Add(ref A, B, operandLabel: nameof(B)),
            () => Add(ref A, C, operandLabel: nameof(C)),
            () => Add(ref A, D, operandLabel: nameof(D)),
            () => Add(ref A, E, operandLabel: nameof(E)),
            () => Add(ref A, H, operandLabel: nameof(H)),
            () => Add(ref A, L, operandLabel: nameof(L)),
            () => Add(ref A, Read(HL), operandLabel: $"({nameof(HL)})"),
            () => Add(ref A, A, operandLabel: nameof(A)),
            () => Add(ref A, B, CarryFlag, operandLabel: nameof(B)),
            () => Add(ref A, C, CarryFlag, operandLabel: nameof(C)),
            () => Add(ref A, D, CarryFlag, operandLabel: nameof(D)),
            () => Add(ref A, E, CarryFlag, operandLabel: nameof(E)),
            () => Add(ref A, H, CarryFlag, operandLabel: nameof(H)),
            () => Add(ref A, L, CarryFlag, operandLabel: nameof(L)),
            () => Add(ref A, Read(HL), CarryFlag, operandLabel: $"({nameof(HL)})"),
            () => Add(ref A, A, CarryFlag, operandLabel: nameof(A)),



            // 0x9X
            () => Subtract(ref A, B, operandLabel: nameof(B)),
            () => Subtract(ref A, C, operandLabel: nameof(C)),
            () => Subtract(ref A, D, operandLabel: nameof(D)),
            () => Subtract(ref A, E, operandLabel: nameof(E)),
            () => Subtract(ref A, H, operandLabel: nameof(H)),
            () => Subtract(ref A, L, operandLabel: nameof(L)),
            () => Subtract(ref A, Read(HL), operandLabel: $"({nameof(HL)})"),
            () => Subtract(ref A, A, operandLabel: nameof(A)),
            () => Subtract(ref A, B, CarryFlag, operandLabel: nameof(B)),
            () => Subtract(ref A, C, CarryFlag, operandLabel: nameof(C)),
            () => Subtract(ref A, D, CarryFlag, operandLabel: nameof(D)),
            () => Subtract(ref A, E, CarryFlag, operandLabel: nameof(E)),
            () => Subtract(ref A, H, CarryFlag, operandLabel: nameof(H)),
            () => Subtract(ref A, L, CarryFlag, operandLabel: nameof(L)),
            () => Subtract(ref A, Read(HL), CarryFlag, operandLabel: $"({nameof(HL)})"),
            () => Subtract(ref A, A, CarryFlag, operandLabel: nameof(A)),



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
            () => Pop(ref B, ref C, targetLabel: nameof(BC)),
            () => ConditionalJumpTo(!ZeroFlag, GetDirectAddress()),
            () => JumpTo(GetDirectAddress()),
            () => ConditionalCall(!ZeroFlag, GetDirectAddress()),
            () => Push(B, C, nameof(BC)),
            () => Add(ref A, Fetch(), operandLabel: $"({nameof(PC)})"),
            () => Restart(0x00),
            () => ConditionalReturn(ZeroFlag),
            Return,
            () => ConditionalJumpTo(ZeroFlag, GetDirectAddress()),
            Prefix_CB,
            () => ConditionalCall(ZeroFlag, GetDirectAddress()),
            () => Call(GetDirectAddress()),
            () => Add(ref A, Fetch(), CarryFlag, operandLabel: $"({nameof(PC)})"),
            () => Restart(0x08),



            // 0xDX
            () => ConditionalReturn(!CarryFlag),
            () => Pop(ref D, ref E, targetLabel: nameof(DE)),
            () => ConditionalJumpTo(!CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(!CarryFlag, GetDirectAddress()),
            () => Push(D, E, nameof(DE)),
            () => Subtract(ref A, Fetch(), operandLabel: $"({nameof(PC)})"),
            () => Restart(0x10),
            () => ConditionalReturn(CarryFlag),
            ReturnAndEnableInterrupt,
            () => ConditionalJumpTo(CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(CarryFlag, GetDirectAddress()),
            Empty,
            () => Subtract(ref A, Fetch(), CarryFlag, operandLabel: $"({nameof(PC)})"),
            () => Restart(0x18),



            // 0xEX
            () => LoadToMem((ushort)(0xFF00 + Fetch()), A),
            () => Pop(ref H, ref L, targetLabel: nameof(HL)),
            () => LoadToMem((ushort)(0xFF00 + C), A),
            Empty,
            Empty,
            () => Push(H, L, nameof(HL)),
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
            () => Load(ref A, Read((ushort)(0xFF00 + Fetch())), nameof(A)),
            () => Pop(ref A, ref F, true, nameof(A) + nameof(F)),
            () => Load(ref A, Read((ushort)(0xFF00 + C)), nameof(A)),
            DisableInterrupt,
            Empty,
            () => Push(A, F, nameof(A) + nameof(F)),
            () => Or(ref A, Fetch()),
            () => Restart(0x30),
            () => { ushort prevSP = SP; AddToStackPointer(Fetch()); Load(ref H, ref L, SP, nameof(HL)); SP = prevSP; },
            () => Load(ref SP, HL, nameof(SP)),
            () => Load(ref A, Read(GetDirectAddress()), nameof(A)),
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