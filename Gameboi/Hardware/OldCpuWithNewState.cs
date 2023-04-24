using System;
using Gameboi.Extensions;
using Gameboi.Memory.Specials;
using static Gameboi.Statics.InterruptAddresses;

namespace Gameboi.Hardware;

public class OldCpuWithNewState
{
    private readonly SystemState state;
    private readonly ImprovedBus bus;

    private InterruptState IE => new(state.InterruptEnableRegister);
    private InterruptState IF => new(state.InterruptFlags);

    private readonly Action[] instructions;
    private readonly byte[] durations;
    private readonly Action[] cbInstructions;

    private CpuFlagRegister Flags => new(state.Flags);

    public bool ZeroFlag => Flags.IsSet(CpuFlags.Zero);
    public bool SubtractFlag => Flags.IsSet(CpuFlags.Subtract);
    public bool HalfCarryFlag => Flags.IsSet(CpuFlags.HalfCarry);
    public bool CarryFlag => Flags.IsSet(CpuFlags.Carry);

    private void SetFlags(bool Z, bool S, bool H, bool C)
    {
        SetZeroFlag(Z);
        SetSubtractFlag(S);
        SetHalfCarryFlag(H);
        SetCarryFlag(C);
    }

    private void SetZeroFlag(bool Z) => SetFlag(CpuFlags.Zero, Z);
    private void SetSubtractFlag(bool S) => SetFlag(CpuFlags.Subtract, S);
    private void SetHalfCarryFlag(bool H) => SetFlag(CpuFlags.HalfCarry, H);
    private void SetCarryFlag(bool C) => SetFlag(CpuFlags.Carry, C);


    private void SetFlag(CpuFlags flags, bool on)
    {
        state.Flags = Flags.SetTo(flags, on);
    }

    public byte Read(ushort address)
    {
        if (state.IsDmaInProgress && address < 0xff00)
        {
            return 0xff;
        }
        return bus.Read(address);
    }

    public void Write(ushort address, byte value)
    {
        if (state.IsDmaInProgress && address < 0xff00)
        {
            return;
        }
        bus.Write(address, value);
    }

    public void Init()
    {
        state.TicksLeftOfInstruction = GetDurationOfNextInstruction();
    }

    public void Tick()
    {
        if (state.TicksLeftOfInstruction is 0)
        {
            if (state.IsHalted)
            {
                HandleInterrupts();
                if (state.IsHalted)
                {
                    state.TicksLeftOfInstruction += durations[0];
                }
                else
                {
                    state.TicksLeftOfInstruction += GetDurationOfNextInstruction();
                }
            }
            else
            {
                var opCode = Fetch();
                instructions[opCode]();

                HandleInterrupts();

                state.TicksLeftOfInstruction += GetDurationOfNextInstruction();
            }
        }

        state.TicksLeftOfInstruction -= 1;
    }

    #region Interrupts
    private void HandleInterrupts()
    {
        if (state.IsInterruptMasterEnablePreparing)
        {
            state.IsInterruptMasterEnablePreparing = false;
            state.InterruptMasterEnable = true;
            return;
        }

        // if IF is 0 there are no interrupt requests => exit
        if (IF.HasNone)
        {
            return;
        }

        // any interrupt request should remove halt-state (even if events are not enabled)
        state.IsHalted = false;

        if (state.InterruptMasterEnable is false
            || IE.HasNone
            || new InterruptState(IF & IE).HasNone)
        {
            return;
        }

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
        state.InterruptMasterEnable = false;
        Write(--state.StackPointer, state.ProgramCounter.GetHighByte());
        // Push of high byte to stack can cancel interrupts
        // But its too late when pushing the low byte
        var tempIF = IF;
        var tempIE = IE;
        Write(--state.StackPointer, state.ProgramCounter.GetLowByte());
        ushort interruptVector = 0;
        if (tempIE.IsVerticalBlankSet && tempIF.IsVerticalBlankSet)
        {

            state.InterruptFlags = IF.WithVerticalBlankUnset();
            interruptVector = VblankVector;
        }
        else if (tempIE.IsLcdStatusSet && tempIF.IsLcdStatusSet)
        {
            state.InterruptFlags = IF.WithLcdStatusUnset();
            interruptVector = LcdStatVector;
        }
        else if (tempIE.IsTimerSet && tempIF.IsTimerSet)
        {
            state.InterruptFlags = IF.WithTimerUnset();
            interruptVector = TimerVector;
        }
        else if (tempIE.IsSerialPortSet && tempIF.IsSerialPortSet)
        {
            state.InterruptFlags = IF.WithSerialPortUnset();
            interruptVector = SerialVector;
        }
        else if (tempIE.IsJoypadSet && tempIF.IsJoypadSet)
        {
            state.InterruptFlags = IF.WithJoypadUnset();
            interruptVector = JoypadVector;
        }
        JumpTo(interruptVector);
        state.TicksLeftOfInstruction += 20;
    }

    public void RequestInterrupt(InterruptType type)
    {
        switch (type)
        {
            case InterruptType.VBlank:
                {
                    state.InterruptFlags = IF.WithVerticalBlankUnset();
                    break;
                }
            case InterruptType.LCDC:
                {
                    state.InterruptFlags = IF.WithLcdStatusUnset();
                    break;
                }
            case InterruptType.Timer:
                {
                    state.InterruptFlags = IF.WithTimerUnset();
                    break;
                }
            case InterruptType.Link:
                {
                    state.InterruptFlags = IF.WithSerialPortUnset();
                    break;
                }
            case InterruptType.Joypad:
                {
                    state.InterruptFlags = IF.WithJoypadUnset();
                    break;
                }
        }
    }

    #endregion


    #region Helpers

    private byte Fetch()
    {
        //Fetch instruction and increment state.ProgramCounter after
        return Read(state.ProgramCounter++);
    }

    private ushort GetDirectAddress()
    {
        byte lowByte = Fetch();
        return Fetch().Concat(lowByte);
    }
    #endregion

    #region Instructions

    #region Misc
    private void Empty() { }
    private void NoOperation() { }
    private void Stop()
    {
        Fetch();
        if (state.SpeedControl.IsBitSet(0))
        {
            state.SpeedControl ^= 0x80;
        }
        //TODO: display white line in center and do nothing untill any button is pressed. 
    }
    private void Halt()
    {
        // Halts CPU until interrupt happens => Perform NOPs meanwhile to not fuck up memory
        state.IsHalted = true;
    }

    private void DisableInterrupt()
    {
        state.InterruptMasterEnable = false;
    }
    private void EnableInterrupt()
    {
        if (state.InterruptMasterEnable)
        {
            return;
        }
        state.IsInterruptMasterEnablePreparing = true;
    }

    private void Prefix_CB()
    {
        byte opCode = Fetch();
        cbInstructions[opCode]();
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
        targetLow = value.GetLowByte();
        targetHigh = value.GetHighByte();
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
        Write(address, source.GetLowByte());
        Write((ushort)(address + 1), source.GetHighByte());
    }
    #endregion

    #region Aritmetic
    private void DAA()
    {
        bool setC = CarryFlag;

        if (SubtractFlag)
        {
            if (CarryFlag)
                state.Accumulator -= 0x60;
            if (HalfCarryFlag)
                state.Accumulator -= 0x6;
        }
        else
        {
            if (CarryFlag || state.Accumulator > 0x99)
            {
                state.Accumulator += 0x60;
                setC = true;
            }
            if (HalfCarryFlag || (state.Accumulator & 0xF) > 0x09)
            {
                state.Accumulator += 0x6;
            }
        }

        SetCarryFlag(setC);
        SetZeroFlag(state.Accumulator == 0);
        SetHalfCarryFlag(false);
    }

    private static ushort Add16(ushort a, ushort b, out bool C, out bool H)
    {
        var result = a + b;
        C = result > 0xFFFF;
        var alow12 = a & 0x0FFF;
        var blow12 = b & 0x0FFF;
        H = alow12 + blow12 > 0x0FFF;
        return (ushort)result;
    }

    private static byte Add8(byte a, byte b, out bool C, out bool H, bool withCarry = false)
    {
        var result = a + b;
        var low4a = a & 0x0F;
        var low4b = b & 0x0F;
        var lowRes = low4a + low4b;
        if (withCarry)
        {
            lowRes++;
            result++;
        }
        H = lowRes > 0x0F;
        C = result > 0xFF;
        return (byte)result;
    }

    private static byte Sub8(ushort a, ushort b, out bool C, out bool H, bool withCarry = false)
    {
        var result = a - b;
        var alow4 = a & 0xF;
        var blow4 = b & 0xF;
        var lowRes = alow4 - blow4;
        if (withCarry)
        {
            result--;
            lowRes--;
        }
        C = result < 0;
        H = lowRes < 0;
        return (byte)result;
    }


    private void Add(ref byte target, byte operand, bool withCarry = false)
    {
        target = Add8(target, operand, out bool C, out bool H, withCarry);
        SetFlags(target == 0, false, H, C);
    }
    private void Add(ref byte targetHigh, ref byte targetLow, byte operandHigh, byte operandLow)
    {
        ushort target = targetHigh.Concat(targetLow);
        ushort operand = operandHigh.Concat(operandLow);
        ushort result = Add16(target, operand, out bool C, out bool H);
        targetHigh = result.GetHighByte();
        targetLow = result.GetLowByte();
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
        var low4 = target & 0xF;
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
        ushort address = addressHigh.Concat(addressLow);
        byte value = Read(address);
        Increment(ref value);
        Write(address, value);
    }

    private void Decrement(ref byte target)
    {
        var low4 = target & 0xF;
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
        ushort address = addresshigh.Concat(addressLow);
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
        target = target.SetBit(bit);
    }

    private void Reset(int bit, ushort address)
    {
        byte value = Read(address);
        Reset(bit, ref value);
        Write(address, value);
    }
    private static void Reset(int bit, ref byte target)
    {
        target = target.UnsetBit(bit);
    }

    private void Bit(int bit, byte source)
    {
        SetZeroFlag(!source.IsBitSet(bit));
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
        target = target.SwapNibbles();
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
            JumpBy(increment);
        }
    }
    private void JumpBy(byte increment) //actually signed
    {
        state.ProgramCounter = (ushort)(state.ProgramCounter + (sbyte)increment);
    }

    private void ConditionalJumpTo(bool condition, ushort address)
    {
        if (condition)
        {
            JumpTo(address);
        }
    }
    private void JumpTo(ushort newPC)
    {
        state.ProgramCounter = newPC;
    }
    private void ConditionalReturn(bool condition)
    {
        if (condition)
        {
            Return();
        }
    }
    private void Return()
    {
        byte newPCLow = Read(state.StackPointer++);
        byte newPCHigh = Read(state.StackPointer++);
        state.ProgramCounter = newPCHigh.Concat(newPCLow);
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
            Call(address);
        }
    }
    private void Call(ushort address)
    {
        Push(state.ProgramCounter.GetHighByte(), state.ProgramCounter.GetLowByte());
        JumpTo(address);
    }
    private void Restart(byte newPC)
    {
        Push(state.ProgramCounter.GetHighByte(), state.ProgramCounter.GetLowByte());
        JumpTo(newPC);
    }
    #endregion

    #region Logic
    private void Complement(ref byte target)
    {
        SetHalfCarryFlag(true);
        SetSubtractFlag(true);
        target = target.Invert();
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
        Write(--state.StackPointer, high);
        Write(--state.StackPointer, low);
    }
    private void Pop(ref byte targetHigh, ref byte targetLow, bool isFlagRegister = false)
    {
        targetLow = Read(state.StackPointer++);
        if (isFlagRegister)
            targetLow &= 0xF0;
        targetHigh = Read(state.StackPointer++);
    }
    private void AddToStackPointer(byte operand)
    {
        //Set flags by add8 and unsigned operand
        Add8(state.StackPointer.GetLowByte(), operand, out bool C, out bool H);
        //treat operand as signed when adding to SP
        state.StackPointer = (ushort)(state.StackPointer + (sbyte)operand);
        SetFlags(false, false, H, C);
    }
    #endregion

    #endregion


    public OldCpuWithNewState(SystemState state, ImprovedBus bus)
    {
        this.state = state;
        this.bus = bus;

        //setup normal instructions
        instructions = new Action[0x100]
        {
            // 0x0X
            NoOperation,
            () => Load(ref state.B, ref state.C, GetDirectAddress()),
            () => LoadToMem(state.BC, state.Accumulator),
            () => Increment(ref state.B, ref state.C),
            () => Increment(ref state.B),
            () => Decrement(ref state.B),
            () => Load(ref state.B, Fetch()),
            () => RotateLeftWithCarry(ref state.Accumulator, false),
            () => LoadToMem(GetDirectAddress(),state.StackPointer),
            () => Add(ref state.High, ref state.Low, state.B, state.C),
            () => Load(ref state.Accumulator, Read(state.BC)),
            () => Decrement(ref state.B, ref state.C),
            () => Increment(ref state.C),
            () => Decrement(ref state.C),
            () => Load(ref state.C, Fetch()),
            () => RotateRightWithCarry(ref state.Accumulator, false),



            // 0x1X
            () => Stop(),
            () => Load(ref state.D, ref state.E, GetDirectAddress()),
            () => LoadToMem(state.DE, state.Accumulator),
            () => Increment(ref state.D, ref state.E),
            () => Increment(ref state.D),
            () => Decrement(ref state.D),
            () => Load(ref state.D, Fetch()),
            () => RotateLeft(ref state.Accumulator, false),
            () => JumpBy(Fetch()),
            () => Add(ref state.High, ref state.Low, state.D, state.E),
            () => Load(ref state.Accumulator, Read(state.DE)),
            () => Decrement(ref state.D, ref state.E),
            () => Increment(ref state.E),
            () => Decrement(ref state.E),
            () => Load(ref state.E, Fetch()),
            () => RotateRight(ref state.Accumulator, false),



            // 0x2X
            () => ConditionalJumpBy(!ZeroFlag, Fetch()),
            () => Load(ref state.High, ref state.Low, GetDirectAddress()),
            () => { LoadToMem(state.HL, state.Accumulator); Increment(ref state.High, ref state.Low); },
            () => Increment(ref state.High, ref state.Low),
            () => Increment(ref state.High),
            () => Decrement(ref state.High),
            () => Load(ref state.High, Fetch()),
            DAA,
            () => ConditionalJumpBy(ZeroFlag, Fetch()),
            () => Add(ref state.High, ref state.Low, state.High, state.Low),
            () => { Load(ref state.Accumulator, Read(state.HL)); Increment(ref state.High, ref state.Low); },
            () => Decrement(ref state.High, ref state.Low),
            () => Increment(ref state.Low),
            () => Decrement(ref state.Low),
            () => Load(ref state.Low, Fetch()),
            () => Complement(ref state.Accumulator),



            // 0x3X
            () => ConditionalJumpBy(!CarryFlag, Fetch()),
            () => Load(ref state.StackPointer, GetDirectAddress()),
            () => { LoadToMem(state.HL, state.Accumulator); Decrement(ref state.High, ref state.Low); },
            () => Increment(ref state.StackPointer),
            () => IncrementInMemory(state.High, state.Low),
            () => DecrementInMemory(state.High, state.Low),
            () => LoadToMem(state.HL, Fetch()),
            SetCarryFlagInstruction,
            () => ConditionalJumpBy(CarryFlag, Fetch()),
            () => Add(ref state.High, ref state.Low, state.StackPointer.GetHighByte(), state.StackPointer.GetLowByte()),
            () => { Load(ref state.Accumulator, Read(state.HL)); Decrement(ref state.High, ref state.Low); },
            () => Decrement(ref state.StackPointer),
            () => Increment(ref state.Accumulator),
            () => Decrement(ref state.Accumulator),
            () => Load(ref state.Accumulator, Fetch()),
            ComplementCarryFlag,



            // 0x4X
            () => Load(ref state.B, state.B),
            () => Load(ref state.B, state.C),
            () => Load(ref state.B, state.D),
            () => Load(ref state.B, state.E),
            () => Load(ref state.B, state.High),
            () => Load(ref state.B, state.Low),
            () => Load(ref state.B, Read(state.HL)),
            () => Load(ref state.B, state.Accumulator),
            () => Load(ref state.C, state.B),
            () => Load(ref state.C, state.C),
            () => Load(ref state.C, state.D),
            () => Load(ref state.C, state.E),
            () => Load(ref state.C, state.High),
            () => Load(ref state.C, state.Low),
            () => Load(ref state.C, Read(state.HL)),
            () => Load(ref state.C, state.Accumulator),



            // 0x5X
            () => Load(ref state.D, state.B),
            () => Load(ref state.D, state.C),
            () => Load(ref state.D, state.D),
            () => Load(ref state.D, state.E),
            () => Load(ref state.D, state.High),
            () => Load(ref state.D, state.Low),
            () => Load(ref state.D, Read(state.HL)),
            () => Load(ref state.D, state.Accumulator),
            () => Load(ref state.E, state.B),
            () => Load(ref state.E, state.C),
            () => Load(ref state.E, state.D),
            () => Load(ref state.E, state.E),
            () => Load(ref state.E, state.High),
            () => Load(ref state.E, state.Low),
            () => Load(ref state.E, Read(state.HL)),
            () => Load(ref state.E, state.Accumulator),



            // 0x6X
            () => Load(ref state.High, state.B),
            () => Load(ref state.High, state.C),
            () => Load(ref state.High, state.D),
            () => Load(ref state.High, state.E),
            () => Load(ref state.High, state.High),
            () => Load(ref state.High, state.Low),
            () => Load(ref state.High, Read(state.HL)),
            () => Load(ref state.High, state.Accumulator),
            () => Load(ref state.Low, state.B),
            () => Load(ref state.Low, state.C),
            () => Load(ref state.Low, state.D),
            () => Load(ref state.Low, state.E),
            () => Load(ref state.Low, state.High),
            () => Load(ref state.Low, state.Low),
            () => Load(ref state.Low, Read(state.HL)),
            () => Load(ref state.Low, state.Accumulator),



            // 0x7X
            () => LoadToMem(state.HL, state.B),
            () => LoadToMem(state.HL, state.C),
            () => LoadToMem(state.HL, state.D),
            () => LoadToMem(state.HL, state.E),
            () => LoadToMem(state.HL, state.High),
            () => LoadToMem(state.HL, state.Low),
            Halt,
            () => LoadToMem(state.HL, state.Accumulator),
            () => Load(ref state.Accumulator, state.B),
            () => Load(ref state.Accumulator, state.C),
            () => Load(ref state.Accumulator, state.D),
            () => Load(ref state.Accumulator, state.E),
            () => Load(ref state.Accumulator, state.High),
            () => Load(ref state.Accumulator, state.Low),
            () => Load(ref state.Accumulator, Read(state.HL)),
            () => Load(ref state.Accumulator, state.Accumulator),



            // 0x8X
            () => Add(ref state.Accumulator, state.B),
            () => Add(ref state.Accumulator, state.C),
            () => Add(ref state.Accumulator, state.D),
            () => Add(ref state.Accumulator, state.E),
            () => Add(ref state.Accumulator, state.High),
            () => Add(ref state.Accumulator, state.Low),
            () => Add(ref state.Accumulator, Read(state.HL)),
            () => Add(ref state.Accumulator, state.Accumulator),
            () => Add(ref state.Accumulator, state.B, CarryFlag),
            () => Add(ref state.Accumulator, state.C, CarryFlag),
            () => Add(ref state.Accumulator, state.D, CarryFlag),
            () => Add(ref state.Accumulator, state.E, CarryFlag),
            () => Add(ref state.Accumulator, state.High, CarryFlag),
            () => Add(ref state.Accumulator, state.Low, CarryFlag),
            () => Add(ref state.Accumulator, Read(state.HL), CarryFlag),
            () => Add(ref state.Accumulator, state.Accumulator, CarryFlag),



            // 0x9X
            () => Subtract(ref state.Accumulator, state.B),
            () => Subtract(ref state.Accumulator, state.C),
            () => Subtract(ref state.Accumulator, state.D),
            () => Subtract(ref state.Accumulator, state.E),
            () => Subtract(ref state.Accumulator, state.High),
            () => Subtract(ref state.Accumulator, state.Low),
            () => Subtract(ref state.Accumulator, Read(state.HL)),
            () => Subtract(ref state.Accumulator, state.Accumulator),
            () => Subtract(ref state.Accumulator, state.B, CarryFlag),
            () => Subtract(ref state.Accumulator, state.C, CarryFlag),
            () => Subtract(ref state.Accumulator, state.D, CarryFlag),
            () => Subtract(ref state.Accumulator, state.E, CarryFlag),
            () => Subtract(ref state.Accumulator, state.High, CarryFlag),
            () => Subtract(ref state.Accumulator, state.Low, CarryFlag),
            () => Subtract(ref state.Accumulator, Read(state.HL), CarryFlag),
            () => Subtract(ref state.Accumulator, state.Accumulator, CarryFlag),



            // 0xAX
            () => And(ref state.Accumulator, state.B),
            () => And(ref state.Accumulator, state.C),
            () => And(ref state.Accumulator, state.D),
            () => And(ref state.Accumulator, state.E),
            () => And(ref state.Accumulator, state.High),
            () => And(ref state.Accumulator, state.Low),
            () => And(ref state.Accumulator, Read(state.HL)),
            () => And(ref state.Accumulator, state.Accumulator),
            () => Xor(ref state.Accumulator, state.B),
            () => Xor(ref state.Accumulator, state.C),
            () => Xor(ref state.Accumulator, state.D),
            () => Xor(ref state.Accumulator, state.E),
            () => Xor(ref state.Accumulator, state.High),
            () => Xor(ref state.Accumulator, state.Low),
            () => Xor(ref state.Accumulator, Read(state.HL)),
            () => Xor(ref state.Accumulator, state.Accumulator),



            // 0xBX
            () => Or(ref state.Accumulator, state.B),
            () => Or(ref state.Accumulator, state.C),
            () => Or(ref state.Accumulator, state.D),
            () => Or(ref state.Accumulator, state.E),
            () => Or(ref state.Accumulator, state.High),
            () => Or(ref state.Accumulator, state.Low),
            () => Or(ref state.Accumulator, Read(state.HL)),
            () => Or(ref state.Accumulator, state.Accumulator),
            () => Compare(state.Accumulator, state.B),
            () => Compare(state.Accumulator, state.C),
            () => Compare(state.Accumulator, state.D),
            () => Compare(state.Accumulator, state.E),
            () => Compare(state.Accumulator, state.High),
            () => Compare(state.Accumulator, state.Low),
            () => Compare(state.Accumulator, Read(state.HL)),
            () => Compare(state.Accumulator, state.Accumulator),



            // 0xCX
            () => ConditionalReturn(!ZeroFlag),
            () => Pop(ref state.B, ref state.C),
            () => ConditionalJumpTo(!ZeroFlag, GetDirectAddress()),
            () => JumpTo(GetDirectAddress()),
            () => ConditionalCall(!ZeroFlag, GetDirectAddress()),
            () => Push(state.B, state.C),
            () => Add(ref state.Accumulator, Fetch()),
            () => Restart(0x00),
            () => ConditionalReturn(ZeroFlag),
            Return,
            () => ConditionalJumpTo(ZeroFlag, GetDirectAddress()),
            Prefix_CB,
            () => ConditionalCall(ZeroFlag, GetDirectAddress()),
            () => Call(GetDirectAddress()),
            () => Add(ref state.Accumulator, Fetch(), CarryFlag),
            () => Restart(0x08),



            // 0xDX
            () => ConditionalReturn(!CarryFlag),
            () => Pop(ref state.D, ref state.E),
            () => ConditionalJumpTo(!CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(!CarryFlag, GetDirectAddress()),
            () => Push(state.D, state.E),
            () => Subtract(ref state.Accumulator, Fetch()),
            () => Restart(0x10),
            () => ConditionalReturn(CarryFlag),
            ReturnAndEnableInterrupt,
            () => ConditionalJumpTo(CarryFlag, GetDirectAddress()),
            Empty,
            () => ConditionalCall(CarryFlag, GetDirectAddress()),
            Empty,
            () => Subtract(ref state.Accumulator, Fetch(), CarryFlag),
            () => Restart(0x18),



            // 0xEX
            () => LoadToMem((ushort)(0xFF00 + Fetch()), state.Accumulator),
            () => Pop(ref state.High, ref state.Low),
            () => LoadToMem((ushort)(0xFF00 + state.C), state.Accumulator),
            Empty,
            Empty,
            () => Push(state.High, state.Low),
            () => And(ref state.Accumulator, Fetch()),
            () => Restart(0x20),
            () => AddToStackPointer(Fetch()),
            () => JumpTo(state.HL),
            () => LoadToMem(GetDirectAddress(), state.Accumulator),
            Empty,
            Empty,
            Empty,
            () => Xor(ref state.Accumulator, Fetch()),
            () => Restart(0x28),



            // 0xFX
            () => Load(ref state.Accumulator, Read((ushort)(0xFF00 + Fetch()))),
            () => Pop(ref state.Accumulator, ref state.Flags, true),
            () => Load(ref state.Accumulator, Read((ushort)(0xFF00 + state.C))),
            DisableInterrupt,
            Empty,
            () => Push(state.Accumulator, state.Flags),
            () => Or(ref state.Accumulator, Fetch()),
            () => Restart(0x30),
            () => { ushort prevSP = state.StackPointer; AddToStackPointer(Fetch()); Load(ref state.High, ref state.Low, state.StackPointer); state.StackPointer = prevSP; },
            () => Load(ref state.StackPointer, state.HL),
            () => Load(ref state.Accumulator, Read(GetDirectAddress())),
            EnableInterrupt,
            Empty,
            Empty,
            () => Compare(state.Accumulator, Fetch()),
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
             8, 8, 8, 8, 8, 8, 4, 8,   4, 4, 4, 4, 4, 4, 8, 4,

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
            () => RotateLeftWithCarry(ref state.B),
            () => RotateLeftWithCarry(ref state.C),
            () => RotateLeftWithCarry(ref state.D),
            () => RotateLeftWithCarry(ref state.E),
            () => RotateLeftWithCarry(ref state.High),
            () => RotateLeftWithCarry(ref state.Low),
            () => RotateLeftWithCarry(state.HL),
            () => RotateLeftWithCarry(ref state.Accumulator),
            () => RotateRightWithCarry(ref state.B),
            () => RotateRightWithCarry(ref state.C),
            () => RotateRightWithCarry(ref state.D),
            () => RotateRightWithCarry(ref state.E),
            () => RotateRightWithCarry(ref state.High),
            () => RotateRightWithCarry(ref state.Low),
            () => RotateRightWithCarry(state.HL),
            () => RotateRightWithCarry(ref state.Accumulator),



            // 0x1X
            () => RotateLeft(ref state.B),
            () => RotateLeft(ref state.C),
            () => RotateLeft(ref state.D),
            () => RotateLeft(ref state.E),
            () => RotateLeft(ref state.High),
            () => RotateLeft(ref state.Low),
            () => RotateLeft(state.HL),
            () => RotateLeft(ref state.Accumulator),
            () => RotateRight(ref state.B),
            () => RotateRight(ref state.C),
            () => RotateRight(ref state.D),
            () => RotateRight(ref state.E),
            () => RotateRight(ref state.High),
            () => RotateRight(ref state.Low),
            () => RotateRight(state.HL),
            () => RotateRight(ref state.Accumulator),



            // 0x2X
            () => ShiftLeftA(ref state.B),
            () => ShiftLeftA(ref state.C),
            () => ShiftLeftA(ref state.D),
            () => ShiftLeftA(ref state.E),
            () => ShiftLeftA(ref state.High),
            () => ShiftLeftA(ref state.Low),
            () => ShiftLeftA(state.HL),
            () => ShiftLeftA(ref state.Accumulator),
            () => ShiftRightA(ref state.B),
            () => ShiftRightA(ref state.C),
            () => ShiftRightA(ref state.D),
            () => ShiftRightA(ref state.E),
            () => ShiftRightA(ref state.High),
            () => ShiftRightA(ref state.Low),
            () => ShiftRightA(state.HL),
            () => ShiftRightA(ref state.Accumulator),


            
            // 0x3X
            () => Swap(ref state.B),
            () => Swap(ref state.C),
            () => Swap(ref state.D),
            () => Swap(ref state.E),
            () => Swap(ref state.High),
            () => Swap(ref state.Low),
            () => Swap(state.HL),
            () => Swap(ref state.Accumulator),
            () => ShiftRightL(ref state.B),
            () => ShiftRightL(ref state.C),
            () => ShiftRightL(ref state.D),
            () => ShiftRightL(ref state.E),
            () => ShiftRightL(ref state.High),
            () => ShiftRightL(ref state.Low),
            () => ShiftRightL(state.HL),
            () => ShiftRightL(ref state.Accumulator),


            
            // 0x4X
            () => Bit(0, state.B),
            () => Bit(0, state.C),
            () => Bit(0, state.D),
            () => Bit(0, state.E),
            () => Bit(0, state.High),
            () => Bit(0, state.Low),
            () => Bit(0, Read(state.HL)),
            () => Bit(0, state.Accumulator),
            () => Bit(1, state.B),
            () => Bit(1, state.C),
            () => Bit(1, state.D),
            () => Bit(1, state.E),
            () => Bit(1, state.High),
            () => Bit(1, state.Low),
            () => Bit(1, Read(state.HL)),
            () => Bit(1, state.Accumulator),



            // 0x5X
            () => Bit(2, state.B),
            () => Bit(2, state.C),
            () => Bit(2, state.D),
            () => Bit(2, state.E),
            () => Bit(2, state.High),
            () => Bit(2, state.Low),
            () => Bit(2, Read(state.HL)),
            () => Bit(2, state.Accumulator),
            () => Bit(3, state.B),
            () => Bit(3, state.C),
            () => Bit(3, state.D),
            () => Bit(3, state.E),
            () => Bit(3, state.High),
            () => Bit(3, state.Low),
            () => Bit(3, Read(state.HL)),
            () => Bit(3, state.Accumulator),



            // 0x6X
            () => Bit(4, state.B),
            () => Bit(4, state.C),
            () => Bit(4, state.D),
            () => Bit(4, state.E),
            () => Bit(4, state.High),
            () => Bit(4, state.Low),
            () => Bit(4, Read(state.HL)),
            () => Bit(4, state.Accumulator),
            () => Bit(5, state.B),
            () => Bit(5, state.C),
            () => Bit(5, state.D),
            () => Bit(5, state.E),
            () => Bit(5, state.High),
            () => Bit(5, state.Low),
            () => Bit(5, Read(state.HL)),
            () => Bit(5, state.Accumulator),



            // 0x7X
            () => Bit(6, state.B),
            () => Bit(6, state.C),
            () => Bit(6, state.D),
            () => Bit(6, state.E),
            () => Bit(6, state.High),
            () => Bit(6, state.Low),
            () => Bit(6, Read(state.HL)),
            () => Bit(6, state.Accumulator),
            () => Bit(7, state.B),
            () => Bit(7, state.C),
            () => Bit(7, state.D),
            () => Bit(7, state.E),
            () => Bit(7, state.High),
            () => Bit(7, state.Low),
            () => Bit(7, Read(state.HL)),
            () => Bit(7, state.Accumulator),



            // 0x8X
            () => Reset(0, ref state.B),
            () => Reset(0, ref state.C),
            () => Reset(0, ref state.D),
            () => Reset(0, ref state.E),
            () => Reset(0, ref state.High),
            () => Reset(0, ref state.Low),
            () => Reset(0, state.HL),
            () => Reset(0, ref state.Accumulator),
            () => Reset(1, ref state.B),
            () => Reset(1, ref state.C),
            () => Reset(1, ref state.D),
            () => Reset(1, ref state.E),
            () => Reset(1, ref state.High),
            () => Reset(1, ref state.Low),
            () => Reset(1, state.HL),
            () => Reset(1, ref state.Accumulator),



            // 0x9X
            () => Reset(2, ref state.B),
            () => Reset(2, ref state.C),
            () => Reset(2, ref state.D),
            () => Reset(2, ref state.E),
            () => Reset(2, ref state.High),
            () => Reset(2, ref state.Low),
            () => Reset(2, state.HL),
            () => Reset(2, ref state.Accumulator),
            () => Reset(3, ref state.B),
            () => Reset(3, ref state.C),
            () => Reset(3, ref state.D),
            () => Reset(3, ref state.E),
            () => Reset(3, ref state.High),
            () => Reset(3, ref state.Low),
            () => Reset(3, state.HL),
            () => Reset(3, ref state.Accumulator),



            // 0xAX
            () => Reset(4, ref state.B),
            () => Reset(4, ref state.C),
            () => Reset(4, ref state.D),
            () => Reset(4, ref state.E),
            () => Reset(4, ref state.High),
            () => Reset(4, ref state.Low),
            () => Reset(4, state.HL),
            () => Reset(4, ref state.Accumulator),
            () => Reset(5, ref state.B),
            () => Reset(5, ref state.C),
            () => Reset(5, ref state.D),
            () => Reset(5, ref state.E),
            () => Reset(5, ref state.High),
            () => Reset(5, ref state.Low),
            () => Reset(5, state.HL),
            () => Reset(5, ref state.Accumulator),



            // 0xBX
            () => Reset(6, ref state.B),
            () => Reset(6, ref state.C),
            () => Reset(6, ref state.D),
            () => Reset(6, ref state.E),
            () => Reset(6, ref state.High),
            () => Reset(6, ref state.Low),
            () => Reset(6, state.HL),
            () => Reset(6, ref state.Accumulator),
            () => Reset(7, ref state.B),
            () => Reset(7, ref state.C),
            () => Reset(7, ref state.D),
            () => Reset(7, ref state.E),
            () => Reset(7, ref state.High),
            () => Reset(7, ref state.Low),
            () => Reset(7, state.HL),
            () => Reset(7, ref state.Accumulator),



            // 0xCX
            () => Set(0, ref state.B),
            () => Set(0, ref state.C),
            () => Set(0, ref state.D),
            () => Set(0, ref state.E),
            () => Set(0, ref state.High),
            () => Set(0, ref state.Low),
            () => Set(0, state.HL),
            () => Set(0, ref state.Accumulator),
            () => Set(1, ref state.B),
            () => Set(1, ref state.C),
            () => Set(1, ref state.D),
            () => Set(1, ref state.E),
            () => Set(1, ref state.High),
            () => Set(1, ref state.Low),
            () => Set(1, state.HL),
            () => Set(1, ref state.Accumulator),



            // 0xDX
            () => Set(2, ref state.B),
            () => Set(2, ref state.C),
            () => Set(2, ref state.D),
            () => Set(2, ref state.E),
            () => Set(2, ref state.High),
            () => Set(2, ref state.Low),
            () => Set(2, state.HL),
            () => Set(2, ref state.Accumulator),
            () => Set(3, ref state.B),
            () => Set(3, ref state.C),
            () => Set(3, ref state.D),
            () => Set(3, ref state.E),
            () => Set(3, ref state.High),
            () => Set(3, ref state.Low),
            () => Set(3, state.HL),
            () => Set(3, ref state.Accumulator),



            // 0xEX
            () => Set(4, ref state.B),
            () => Set(4, ref state.C),
            () => Set(4, ref state.D),
            () => Set(4, ref state.E),
            () => Set(4, ref state.High),
            () => Set(4, ref state.Low),
            () => Set(4, state.HL),
            () => Set(4, ref state.Accumulator),
            () => Set(5, ref state.B),
            () => Set(5, ref state.C),
            () => Set(5, ref state.D),
            () => Set(5, ref state.E),
            () => Set(5, ref state.High),
            () => Set(5, ref state.Low),
            () => Set(5, state.HL),
            () => Set(5, ref state.Accumulator),



            // 0xFX
            () => Set(6, ref state.B),
            () => Set(6, ref state.C),
            () => Set(6, ref state.D),
            () => Set(6, ref state.E),
            () => Set(6, ref state.High),
            () => Set(6, ref state.Low),
            () => Set(6, state.HL),
            () => Set(6, ref state.Accumulator),
            () => Set(7, ref state.B),
            () => Set(7, ref state.C),
            () => Set(7, ref state.D),
            () => Set(7, ref state.E),
            () => Set(7, ref state.High),
            () => Set(7, ref state.Low),
            () => Set(7, state.HL),
            () => Set(7, ref state.Accumulator),
        };
    }

    private int GetDurationOfNextInstruction()
    {
        var opCode = Read(state.ProgramCounter);
        return opCode switch
        {
            0x20 => Flags.IsNotSet(CpuFlags.Zero) ? 12 : 8,
            0x28 => Flags.IsSet(CpuFlags.Zero) ? 12 : 8,
            0x30 => Flags.IsNotSet(CpuFlags.Carry) ? 12 : 8,
            0x38 => Flags.IsSet(CpuFlags.Carry) ? 12 : 8,
            0xc0 => Flags.IsNotSet(CpuFlags.Zero) ? 20 : 8,
            0xc2 => Flags.IsNotSet(CpuFlags.Zero) ? 16 : 12,
            0xc4 => Flags.IsNotSet(CpuFlags.Zero) ? 24 : 12,
            0xc8 => Flags.IsSet(CpuFlags.Zero) ? 20 : 8,
            0xca => Flags.IsSet(CpuFlags.Zero) ? 16 : 12,
            0xcb => GetCbDuration(),
            0xcc => Flags.IsSet(CpuFlags.Zero) ? 24 : 12,
            0xd0 => Flags.IsNotSet(CpuFlags.Carry) ? 20 : 8,
            0xd2 => Flags.IsNotSet(CpuFlags.Carry) ? 16 : 12,
            0xd4 => Flags.IsNotSet(CpuFlags.Carry) ? 24 : 12,
            0xd8 => Flags.IsSet(CpuFlags.Carry) ? 20 : 8,
            0xda => Flags.IsSet(CpuFlags.Carry) ? 16 : 12,
            0xdc => Flags.IsSet(CpuFlags.Carry) ? 24 : 12,
            _ => durations[opCode]
        };
    }

    private int GetCbDuration()
    {
        var cbOpCode = Read((ushort)(state.ProgramCounter + 1));
        var modded = cbOpCode % 8;
        var duration = modded == 6 ? 16 : 8;
        return duration + 4;
    }

    // GB-docs source: http://marc.rawer.state.DE/Gameboy/Docs/GBCPUman.pdf
    // Instructions matrix: https://pastraiser.com/cpu/gameboy/gameboy_opcodes.html 

}
