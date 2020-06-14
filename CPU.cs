using static ByteOperations;

public enum InterruptType
{
    VBlank,
    LCDC,
    Timer,
    Link,
    Joypad
}

class CPU : Hardware<MainBus>
{
    private bool IME = true; // Interrupt Master Enable
    private const ushort IE_address = 0xFFFF;
    private const ushort IF_address = 0xFF0F;

    private bool isHalted = false;
    private bool isStopped = false;

    private delegate void Instruction();

    private Instruction[] instructions;
    private byte[] durations;
    private Instruction[] cbInstructions;

    #region Registers
    private byte A; // accumulator
    private byte F; // flag register
    private byte B;
    private byte C;
    private ushort BC => ConcatBytes(B, C);
    private byte D;
    private byte E;
    private ushort DE => ConcatBytes(D, E);
    private byte H;
    private byte L;
    private ushort HL => ConcatBytes(H, L);
    private ushort PC; //progarm counter
    private byte PC_P => GetHighByte(PC);
    private byte PC_C => GetLowByte(PC);
    private ushort SP; //stack pointer
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


    ulong cycles = 0;
    long instructionsPerformed = 0;

    public ulong Tick()
    {
        instructionsPerformed++;

        HandleInterrupts();

        if (isHalted)
        {
            NoOperation();
            cycles += 4;
        }
        else
        {
            // Fetch, Decode, Execute
            byte opCode = Fetch();
            instructions[opCode]();
            cycles += durations[opCode];
        }
        return cycles;
    }


    #region Interrupts
    private const byte V_Blank_bit = 0;
    private const byte LCDC_bit = 1;
    private const byte Timer_bit = 2;
    private const byte Link_bit = 3;
    private const byte Joypad_bit = 4;

    private void HandleInterrupts()
    {
        if (!IME) return;

        byte IF = Read(IF_address);

        // if IF is 0 there are no interrupt requests => exit
        if (IF == 0) return;

        byte IE = Read(IE_address);

        // type             :   prio    : address   : bit
        //---------------------------------------------------
        // V-Blank          :     1     : 0x0040    : 0
        // LCDC Status      :     2     : 0x0048    : 1
        // Timer Overflow   :     3     : 0x0050    : 2
        // Serial Transfer  :     4     : 0x0058    : 3
        // Hi-Lo of P10-P13 :     5     : 0x0060    : 4

        if (ShouldInterrupt(IE, IF, V_Blank_bit))
        {
            Interrupt(0x0040, V_Blank_bit, IF);
        }
        else if (ShouldInterrupt(IE, IF, LCDC_bit))
        {
            Interrupt(0x0048, LCDC_bit, IF);
        }
        else if (ShouldInterrupt(IE, IF, Timer_bit))
        {
            Interrupt(0x0050, Timer_bit, IF);
        }
        else if (ShouldInterrupt(IE, IF, Link_bit))
        {
            Interrupt(0x0058, Link_bit, IF);
        }
        else if (ShouldInterrupt(IE, IF, Joypad_bit))
        {
            Interrupt(0x0060, Joypad_bit, IF);
        }
    }

    private void Interrupt(ushort startingAddress, byte bit, byte IF)
    {
        IME = false;
        Write(IF_address, ResetBit(bit, IF)); // remove the interrupt request that is granted 
        Push(PC_P, PC_C);
        JumpTo(startingAddress);
    }

    private bool ShouldInterrupt(byte IE, byte IF, byte bit)
    {
        return TestBit(bit, IE) && TestBit(bit, IF);
    }

    public void RequestInterrupt(InterruptType type)
    {
        switch (type)
        {
            case InterruptType.VBlank:
                {
                    SetInterruptRequest(V_Blank_bit);
                    break;
                }
            case InterruptType.LCDC:
                {
                    SetInterruptRequest(LCDC_bit);
                    break;
                }
            case InterruptType.Timer:
                {
                    SetInterruptRequest(Timer_bit);
                    break;
                }
            case InterruptType.Link:
                {
                    SetInterruptRequest(Link_bit);
                    break;
                }
            case InterruptType.Joypad:
                {
                    SetInterruptRequest(Joypad_bit);
                    break;
                }
        }
    }

    private void SetInterruptRequest(int bit)
    {
        Write(IF_address, SetBit(bit, Read(IF_address)));
    }
    #endregion


    #region Helpers
    public override void Connect(MainBus bus)
    {
        bus.ConnectCPU(this);
        base.Connect(bus);
    }
    private byte Fetch()
    {
        //Fetch instruction and increment PC after
        return Read(PC++);
    }

    private ushort GetDirectAddress()
    {
        return ConcatBytes(Fetch(), Fetch());
    }

    private byte Read(ushort address)
    {
        if (!bus.Read(address, out byte value))
            throw new MemoryReadException(address);
        return value;
    }
    private void Write(ushort address, byte value)
    {
        if (!bus.Write(address, value))
            throw new MemoryWriteException(address);
    }
    #endregion


    #region Instructions

    #region Misc
    private void Empty() { }
    private void NoOperation() { }
    private void Stop(byte arg)
    {
        //TODO: display white line in center and do nothing untill any button is pressed. 
        isStopped = true;
    }
    private void Halt()
    {
        // Halts CPU until interrupt happens => Perform NOPs meanwhile to not fuck up memory
        isHalted = true;
    }

    private void DisableInterrupt()
    {
        IME = false;
    }
    private void EnableInterrupt()
    {
        IME = true;
    }

    private void Prefix_CB()
    {
        byte opCode = Fetch();
        cbInstructions[opCode]();
        byte modded = (byte)(opCode % 8);
        byte duration = (byte)(modded == 6 ? 16 : 8);
        cycles += duration;
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
        SetHalfCarryFlag(CarryFlag); // Z80 doc says copy carry flag, gameboy-doc says reset...
        SetCarryFlag(!CarryFlag);
    }
    #endregion

    #region Loads
    private void Load(ref byte target, byte source)
    {
        target = source;
    }
    private void Load(ref byte targetHigh, ref byte targetLow, ushort value)
    {
        targetLow = GetLowByte(value);
        targetHigh = GetHighByte(value);
    }
    private void Load(ref ushort target, ushort value)
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
        //an attempt at implementing this...
        byte highNibble = (byte)((A & 0xF0) >> 4);
        byte lowNibble = (byte)(A & 0x0F);
        byte correction = 0;
        bool setC = CarryFlag;

        if (SubtractFlag)
        {
            if (!CarryFlag && HalfCarryFlag && highNibble < 9 && lowNibble > 5)
            {
                correction = 0xFA;
            }
            else if (CarryFlag && !HalfCarryFlag && highNibble > 6 && lowNibble < 0xA)
            {
                correction = 0xA0;
            }
            else if (CarryFlag && HalfCarryFlag && (highNibble == 6 || highNibble == 7) && lowNibble > 5)
            {
                correction = 0x9A;
            }
        }
        else
        {
            if (!CarryFlag && !HalfCarryFlag && highNibble < 9 && lowNibble > 9)
            {
                correction = 6;
            }
            else if (!CarryFlag && HalfCarryFlag && highNibble < 0xA && lowNibble < 4)
            {
                correction = 6;
            }
            else if (!CarryFlag && !HalfCarryFlag && highNibble > 9 && lowNibble < 0xA)
            {
                correction = 0x60;
                setC = true;
            }
            else if (!CarryFlag && !HalfCarryFlag && highNibble > 8 && lowNibble > 9)
            {
                correction = 0x66;
                setC = true;
            }
            else if (!CarryFlag && HalfCarryFlag && highNibble > 9 && lowNibble < 4)
            {
                correction = 0x66;
                setC = true;
            }
            else if (CarryFlag && !HalfCarryFlag && highNibble < 3 && lowNibble < 0xA)
            {
                correction = 0x60;
            }
            else if (CarryFlag && !HalfCarryFlag && highNibble < 3 && lowNibble > 9)
            {
                correction = 0x66;
            }
            else if (CarryFlag && HalfCarryFlag && highNibble < 4 && lowNibble < 4)
            {
                correction = 0x66;
            }
        }

        int correctedValue = A + correction;
        setC = setC || SubtractFlag ? correctedValue > 0xFF : correctedValue < 0;

        A = (byte)correctedValue;

        SetCarryFlag(setC);
        SetZeroFlag(A == 0);
        SetHalfCarryFlag(false);
    }

    private void Add(ref byte target, byte operand)
    {
        bool H = IsHalfCarryOnAddition(target, operand);
        int result = target + operand;
        bool C = IsCarryOnAddition(result);
        SetFlags(result == 0, false, H, C);
        target = (byte)result;
    }
    private void Add(ref byte targetHigh, ref byte targetLow, byte operandHigh, byte operandLow)
    {
        int resultLow = targetLow + operandLow;
        int carryLow = IsCarryOnAddition(resultLow) ? 1 : 0;
        int resultHigh = targetHigh + operandHigh + carryLow;
        bool isCarryHigh = IsCarryOnAddition(resultHigh);
        SetCarryFlag(isCarryHigh);
        SetSubtractFlag(false);
        byte highAddition = (byte)(carryLow + operandHigh);
        SetHalfCarryFlag(IsHalfCarryOnAddition(targetHigh, highAddition));
        targetLow = (byte)resultLow;
        targetHigh = (byte)resultHigh;
    }
    private void Add(ref byte targetHigh, ref byte targetLow, ushort operand)
    {
        byte operandHigh = GetHighByte(operand);
        byte operandLow = GetLowByte(operand);
        Add(ref targetHigh, ref targetLow, operandHigh, operandLow);
    }
    private void AddWithCarry(ref byte target, byte operand)
    {
        byte newOperand = (byte)(operand + (CarryFlag ? 1 : 0));
        Add(ref target, newOperand);
    }

    private void Subtract(ref byte target, byte operand)
    {
        int result = target - operand;
        SetFlags(result == 0, true, IsHalfCarryOnSubtraction(target, operand), result < 0);
        target = (byte)result;
    }
    private void SubtractWithCarry(ref byte target, byte operand)
    {
        byte newOperand = (byte)(operand + (CarryFlag ? 1 : 0));
        Subtract(ref target, newOperand);
    }

    private void Increment(ref byte target)
    {
        SetHalfCarryFlag(IsHalfCarryOnAddition(target, 1)); // set if carry from bit 3
        target++;
        SetZeroFlag(target == 0);
        SetSubtractFlag(false);
    }
    private void Increment(ref byte targetHigh, ref byte targetLow)
    {
        int newLowByte = targetLow + 1;
        if (newLowByte > 0xFF)
        {
            targetHigh++;
        }
        targetLow = (byte)newLowByte;
    }
    private void Increment(ref ushort target)
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
        SetHalfCarryFlag(IsHalfCarryOnSubtraction(target, 1)); // set if borrow from bit 4
        target--;
        SetZeroFlag(target == 0);
        SetSubtractFlag(true);
    }
    private void Decrement(ref byte targetHigh, ref byte targetLow)
    {
        int newLowByte = targetLow - 1;
        if (newLowByte < 0)
        {
            targetHigh--;
        }
        targetLow = (byte)newLowByte;
    }
    private void Decrement(ref ushort target)
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
        bool isCarry = IsCarryOnAddition(rotated);
        SetFlags(cb_mode ? rotated == 0 : false, false, false, isCarry);
        if (isCarry)
            target = (byte)(rotated | 1); //wrap around carry bit
        else
            target = (byte)rotated; //no need for wrap around
    }
    private void RotateRightWithCarry(ref byte target, bool cb_mode = true)
    {
        bool isCarry = (target & 1) != 0;
        int rotated = target >> 1;
        SetFlags(false, false, false, isCarry);
        if (isCarry)
            target = (byte)(rotated | 0x80);
        else
            target = (byte)rotated;
    }
    private void RotateLeft(ref byte target, bool cb_mode = true)
    {
        int rotated = target << 1;
        bool oldIsCarry = CarryFlag;
        bool isCarry = IsCarryOnAddition(rotated);
        SetFlags(cb_mode ? rotated == 0 : false, false, false, isCarry);
        if (oldIsCarry)
            target = (byte)(rotated | 1);
        else
            target = (byte)rotated;
    }
    private void RotateRight(ref byte target, bool cb_mode = true)
    {
        bool oldIsCarry = CarryFlag;
        bool isCarry = (target & 1) != 0;
        int rotated = target >> 1;
        SetFlags(cb_mode ? rotated == 0 : false, false, false, isCarry);
        if (oldIsCarry)
            target = (byte)(rotated | 0x80);
        else
            target = (byte)rotated;
    }

    private void Set(int bit, ushort address)
    {
        byte value = Read(address);
        Set(bit, ref value);
        Write(address, value);
    }
    private void Set(int bit, ref byte target)
    {
        target = SetBit(bit, target);
    }

    private void Reset(int bit, ushort address)
    {
        byte value = Read(address);
        Reset(bit, ref value);
        Write(address, value);
    }
    private void Reset(int bit, ref byte target)
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
        SetFlags(value == 0, false, false, false);
    }
    private void Swap(ref byte target)
    {
        target = SwapNibbles(target);
    }

    private void ShiftLeftA(ushort address)
    {
        byte value = Read(address);
        ShiftLeftA(ref value);
        Write(address, value);
    }
    private void ShiftLeftA(ref byte target)
    {
        int shifted = (target << 1);
        SetFlags(shifted == 0, false, false, (target & 0x80) == 0x80);
        target = (byte)shifted;
    }
    private void ShiftRightA(ushort address)
    {
        byte value = Read(address);
        ShiftRightA(ref value);
        Write(address, value);
    }
    private void ShiftRightA(ref byte target)
    {
        int shifted = (target >> 1);
        SetFlags(shifted == 0, false, false, (target & 0x01) == 0x01);
        target = (byte)(shifted | (target & 0x80));
    }
    private void ShiftRightL(ushort address)
    {
        byte value = Read(address);
        ShiftRightL(ref value);
        Write(address, value);
    }
    private void ShiftRightL(ref byte target)
    {
        int shifted = (target >> 1);
        SetFlags(shifted == 0, false, false, (target & 1) == 1);
        target = (byte)shifted;
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
            cycles += 4;
            JumpBy(increment);
        }
    }
    private void JumpBy(byte increment) //actually signed
    {
        PC = (byte)(PC + (sbyte)increment);
    }
    private void ConditionalJumpTo(bool condition, ushort address)
    {
        if (condition)
        {
            cycles += 4;
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
            cycles += 12;
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
            cycles += 12;
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
        int result = target - operand;
        SetFlags(result == 0, true, IsHalfCarryOnSubtraction(target, operand), result < 0);
    }
    #endregion

    #region Stack Interaction
    private void Push(byte high, byte low)
    {
        Write(--SP, high);
        Write(--SP, low);
    }
    private void Pop(ref byte targetHigh, ref byte targetLow)
    {
        targetLow = Read(SP++);
        targetHigh = Read(SP++);
    }
    private void AddToStackPointer(sbyte operand)
    {
        int result = (SP + operand);
        SetFlags(
            false,
            false,
            operand < 0 ? IsHalfCarryOnSubtraction(SP_P, (byte)operand) : IsHalfCarryOnAddition(SP_P, (byte)operand),
            operand < 0 ? IsCarryOnSubtraction(result) : IsCarryOnAddition(result)
        );
        SP = (ushort)result;
    }
    #endregion

    #endregion


    public CPU()
    {
        //setup normal instructions
        instructions = new Instruction[0x100]
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
            () => ConditionalJumpBy(!ZeroFlag,Fetch()),
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
            () => Add(ref H, ref L, SP),
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
            () => AddWithCarry(ref A, B),
            () => AddWithCarry(ref A, C),
            () => AddWithCarry(ref A, D),
            () => AddWithCarry(ref A, E),
            () => AddWithCarry(ref A, H),
            () => AddWithCarry(ref A, L),
            () => AddWithCarry(ref A, Read(HL)),
            () => AddWithCarry(ref A, A),



            // 0x9X
            () => Subtract(ref A, B),
            () => Subtract(ref A, C),
            () => Subtract(ref A, D),
            () => Subtract(ref A, E),
            () => Subtract(ref A, H),
            () => Subtract(ref A, L),
            () => Subtract(ref A, Read(HL)),
            () => Subtract(ref A, A),
            () => SubtractWithCarry(ref A, B),
            () => SubtractWithCarry(ref A, C),
            () => SubtractWithCarry(ref A, D),
            () => SubtractWithCarry(ref A, E),
            () => SubtractWithCarry(ref A, H),
            () => SubtractWithCarry(ref A, L),
            () => SubtractWithCarry(ref A, Read(HL)),
            () => SubtractWithCarry(ref A, A),



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
            () => AddWithCarry(ref A, Fetch()),
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
            () => SubtractWithCarry(ref A, Fetch()),
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
            () => AddToStackPointer((sbyte)Fetch()),
            () => JumpTo(Read(HL)),
            () => LoadToMem(GetDirectAddress(), A),
            Empty,
            Empty,
            Empty,
            () => Xor(ref A, Fetch()),
            () => Restart(0x28),



            // 0xFX
            () => Load(ref A, Read((ushort)(0xFF00 + Fetch()))),
            () => Pop(ref A, ref F),
            () => Load(ref A, Read((ushort)(0xFF00 + C))),
            DisableInterrupt,
            Empty,
            () => Push(A, F),
            () => Or(ref A, Fetch()),
            () => Restart(0x30),
            () => { ushort prevSP = SP; AddToStackPointer((sbyte)Fetch()); Load(ref H, ref L, SP); SP = prevSP; },
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
        cbInstructions = new Instruction[0x100]
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



    /*
    Old PerformInstruction
    switch (opCode)
            {
                //intruction format: Name bytes cycles

                case 0x00:
                    {
                        //NOP | 1 | 4
                        NoOperation();
                        break;
                    }

                case 0x01:
                    {
                        // LD BC, d16 | 3 | 12
                        Load(ref B, ref C, ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0x02:
                    {
                        // LD (BC), A | 1 | 8
                        ushort address = ConcatBytes(B, C);
                        LoadToMem(address, A);
                        break;
                    }

                case 0x03:
                    {
                        // INC BC | 1 | 8 
                        Increment(ref B, ref C);
                        break;
                    }

                case 0x04:
                    {
                        // INC B | 1 | 4
                        // Z 0 H -
                        Increment(ref B);
                        break;
                    }

                case 0x05:
                    {
                        // DEC B | 1 | 4
                        // Z 1 H -
                        Decrement(ref B);
                        break;
                    }

                case 0x06:
                    {
                        // LD B, d8 | 2 | 8
                        Load(ref B, Fetch());
                        break;
                    }

                case 0x07:
                    {
                        // RLCA | 1 | 4
                        // 0 0 0 C
                        RotateLeftWithCarry(ref A);
                        break;
                    }

                case 0x08:
                    {
                        // LD (a16), SP | 3 | 20
                        ushort address = ConcatBytes(Fetch(), Fetch());
                        LoadToMem(address, SP);
                        break;
                    }
                case 0x09:
                    {
                        // ADD HL, BC | 1 | 8
                        // - 0 H C
                        Add(ref H, ref L, B, C);
                        break;
                    }

                case 0x0A:
                    {
                        // LD A, (BC) | 1 | 8
                        ushort address = ConcatBytes(B, C);
                        LoadFromMem(ref A, address);
                        break;
                    }
                case 0x0B:
                    {
                        // DEC BC | 1 | 8
                        Decrement(ref B, ref C);
                        break;
                    }
                case 0x0C:
                    {
                        // INC C | 1 | 4
                        // Z 0 H -
                        Increment(ref C);
                        break;
                    }
                case 0x0D:
                    {
                        // DEC C | 1 | 4
                        // Z 1 H -
                        Decrement(ref C);
                        break;
                    }
                case 0x0E:
                    {
                        // LD C, d8 | 2 | 8
                        Load(ref C, Fetch());
                        break;
                    }
                case 0x0F:
                    {
                        // RRCA | 1 | 4
                        // 0 0 0 C
                        RotateRightWithCarry(ref A);
                        break;
                    }

                case 0x10:
                    {
                        // STOP 0 | 2 | 4
                        Stop(Fetch());
                        break;
                    }
                case 0x11:
                    {
                        // LD DE, d16 | 3 | 12 
                        Load(ref D, ref E, ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0x12:
                    {
                        // LD (DE), A | 1 | 8
                        LoadToMem(ConcatBytes(D, E), A);
                        break;
                    }

                case 0x13:
                    {
                        // INC DE
                        Increment(ref D, ref C);
                        break;
                    }
                case 0x14:
                    {
                        // INC D
                        Increment(ref D);
                        break;
                    }
                case 0x15:
                    {
                        //DEC D
                        Decrement(ref D);
                        break;
                    }
                case 0x16:
                    {
                        // LD D, d8
                        Load(ref D, Fetch());
                        break;
                    }
                case 0x17:
                    {
                        // RLA | 1 | 4
                        RotateLeft(ref A);
                        break;
                    }
                case 0x18:
                    {
                        // JR r8 | 2 | 12
                        JumpBy(Fetch());
                        break;
                    }
                case 0x19:
                    {
                        // ADD HL, DE
                        Add(ref H, ref L, D, E);
                        break;
                    }
                case 0x1A:
                    {
                        // LD A, (DE)
                        LoadFromMem(ref A, ConcatBytes(D, E));
                        break;
                    }
                case 0x1B:
                    {
                        // DEC DE
                        Decrement(ref D, ref E);
                        break;
                    }
                case 0x1C:
                    {
                        // INC E
                        Increment(ref E);
                        break;
                    }
                case 0x1D:
                    {
                        // DEC E
                        Decrement(ref E);
                        break;
                    }
                case 0x1E:
                    {
                        // LD E, d8
                        Load(ref E, Fetch());
                        break;
                    }
                case 0x1F:
                    {
                        // RRA | 1 | 4
                        RotateRight(ref A);
                        break;
                    }
                case 0x20:
                    {
                        // JR NZ, r8
                        if (!ZeroFlag)
                            JumpBy(Fetch());
                        break;
                    }
                case 0x21:
                    {
                        // LD HL, d16 | 3 | 12 
                        Load(ref H, ref L, ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0x22:
                    {
                        // LD (HL+), A
                        LoadToMem(HL, A);
                        Increment(ref H, ref L);
                        break;
                    }

                case 0x23:
                    {
                        // INC HL
                        Increment(ref H, ref L);
                        break;
                    }
                case 0x24:
                    {
                        // INC H
                        Increment(ref H);
                        break;
                    }
                case 0x25:
                    {
                        //DEC H
                        Decrement(ref H);
                        break;
                    }
                case 0x26:
                    {
                        // LD H, d8
                        Load(ref H, Fetch());
                        break;
                    }
                case 0x27:
                    {
                        // DAA | 1 | 4
                        //TODO: FUCK THAT
                        break;
                    }
                case 0x28:
                    {
                        // JR Z, r8
                        if (ZeroFlag) JumpBy(Fetch());
                        break;
                    }
                case 0x29:
                    {
                        // ADD HL, HL
                        Add(ref H, ref L, H, L);
                        break;
                    }
                case 0x2A:
                    {
                        // LD A, (HL+)
                        LoadFromMem(ref A, HL);
                        Increment(ref H, ref L);
                        break;
                    }
                case 0x2B:
                    {
                        // DEC HL
                        Decrement(ref H, ref L);
                        break;
                    }
                case 0x2C:
                    {
                        // INC L
                        Increment(ref L);
                        break;
                    }
                case 0x2D:
                    {
                        // DEC L
                        Decrement(ref L);
                        break;
                    }
                case 0x2E:
                    {
                        // LD L, d8
                        Load(ref L, Fetch());
                        break;
                    }
                case 0x2F:
                    {
                        // CPL | 1 | 4
                        Complement(ref A);
                        break;
                    }
                case 0x30:
                    {
                        // JR NC, r8 | 2 | 12/8
                        if (!CarryFlag) JumpBy(Fetch());
                        break;
                    }
                case 0x31:
                    {
                        // LD SP, d16
                        Load(ref SP, ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0x32:
                    {
                        // LD (HL-), A
                        LoadToMem(HL, A);
                        Decrement(ref H, ref L);
                        break;
                    }

                case 0x33:
                    {
                        //INC SP
                        Increment(ref SP);
                        break;
                    }
                case 0x34:
                    {
                        // INC (HL)
                        IncrementInMemory(H, L);
                        break;
                    }
                case 0x35:
                    {
                        // DEC (HL)
                        DecrementInMemory(H, L);
                        break;
                    }
                case 0x36:
                    {
                        // LD (HL), d8
                        LoadToMem(HL, Fetch());
                        break;
                    }
                case 0x37:
                    {
                        // SCF | 1 | 4 (Set Carry Flag)
                        SetCarryFlagInstruction();
                        break;
                    }
                case 0x38:
                    {
                        // JR C, r8
                        if (CarryFlag) JumpBy(Fetch());
                        break;
                    }
                case 0x39:
                    {
                        // ADD HL, SP
                        Add(ref H, ref L, SP);
                        break;
                    }
                case 0x3A:
                    {
                        // LD A, (HL-)

                        Load(ref A, Read(HL));
                        Decrement(ref H, ref L);
                        break;
                    }
                case 0x3B:
                    {
                        // DEC SP
                        Decrement(ref SP);
                        break;
                    }
                case 0x3C:
                    {
                        // INC A
                        Increment(ref A);
                        break;
                    }
                case 0x3D:
                    {
                        // DEC A
                        Decrement(ref A);
                        break;
                    }
                case 0x3E:
                    {
                        // LD A, d8
                        Load(ref A, Fetch());
                        break;
                    }
                case 0x3F:
                    {
                        // CCF
                        ComplementCarryFlag();
                        break;
                    }
                case 0x40:
                    {
                        // LD B, B
                        Load(ref B, B);
                        break;
                    }
                case 0x41:
                    {
                        // LD B, C
                        Load(ref B, C);
                        break;
                    }

                case 0x42:
                    {
                        // LD B, D
                        Load(ref B, D);
                        break;
                    }

                case 0x43:
                    {
                        // LD B, E
                        Load(ref B, E);
                        break;
                    }
                case 0x44:
                    {
                        // LD B, H
                        Load(ref B, H);
                        break;
                    }
                case 0x45:
                    {
                        // LD B, L
                        Load(ref B, L);
                        break;
                    }
                case 0x46:
                    {
                        // LD B, (HL)
                        LoadFromMem(ref B, HL);
                        break;
                    }
                case 0x47:
                    {
                        // LD B, A
                        Load(ref B, A);
                        break;
                    }
                case 0x48:
                    {
                        // LD C, B
                        Load(ref C, B);
                        break;
                    }
                case 0x49:
                    {
                        // LD C, C
                        Load(ref C, C);
                        break;
                    }
                case 0x4A:
                    {
                        // LD C, D
                        Load(ref C, D);
                        break;
                    }
                case 0x4B:
                    {
                        // LD C, E
                        Load(ref C, E);
                        break;
                    }
                case 0x4C:
                    {
                        // LD C, H
                        Load(ref C, H);
                        break;
                    }
                case 0x4D:
                    {
                        // LD C, L
                        Load(ref C, L);
                        break;
                    }
                case 0x4E:
                    {
                        // LD C, (HL)
                        LoadFromMem(ref C, HL);
                        break;
                    }
                case 0x4F:
                    {
                        // LD C, A
                        Load(ref C, A);
                        break;
                    }
                case 0x50:
                    {
                        // LD D, B
                        Load(ref D, B);
                        break;
                    }
                case 0x51:
                    {
                        Load(ref D, C);
                        break;
                    }

                case 0x52:
                    {
                        Load(ref D, D);
                        break;
                    }

                case 0x53:
                    {
                        Load(ref D, E);
                        break;
                    }
                case 0x54:
                    {
                        Load(ref D, H);
                        break;
                    }
                case 0x55:
                    {
                        Load(ref D, L);
                        break;
                    }
                case 0x56:
                    {
                        LoadFromMem(ref D, HL);
                        break;
                    }
                case 0x57:
                    {
                        Load(ref D, A);
                        break;
                    }
                case 0x58:
                    {
                        // LD E ...
                        Load(ref E, B);
                        break;
                    }
                case 0x59:
                    {
                        Load(ref E, C);
                        break;
                    }
                case 0x5A:
                    {
                        Load(ref E, D);
                        break;
                    }
                case 0x5B:
                    {
                        Load(ref E, E);
                        break;
                    }
                case 0x5C:
                    {
                        Load(ref E, H);
                        break;
                    }
                case 0x5D:
                    {
                        Load(ref E, L);
                        break;
                    }
                case 0x5E:
                    {
                        LoadFromMem(ref E, HL);
                        break;
                    }
                case 0x5F:
                    {
                        Load(ref E, A);
                        break;
                    }
                case 0x60:
                    {
                        // LD H ...
                        Load(ref H, B);
                        break;
                    }
                case 0x61:
                    {
                        Load(ref H, C);
                        break;
                    }

                case 0x62:
                    {
                        Load(ref H, D);
                        break;
                    }

                case 0x63:
                    {
                        Load(ref H, E);
                        break;
                    }
                case 0x64:
                    {
                        Load(ref H, H);
                        break;
                    }
                case 0x65:
                    {
                        Load(ref H, L);
                        break;
                    }
                case 0x66:
                    {
                        LoadFromMem(ref H, HL);
                        break;
                    }
                case 0x67:
                    {
                        Load(ref H, A);
                        break;
                    }
                case 0x68:
                    {
                        // LD L ...
                        Load(ref L, B);
                        break;
                    }
                case 0x69:
                    {
                        Load(ref L, C);
                        break;
                    }
                case 0x6A:
                    {
                        Load(ref L, D);
                        break;
                    }
                case 0x6B:
                    {
                        Load(ref L, E);
                        break;
                    }
                case 0x6C:
                    {
                        Load(ref L, H);
                        break;
                    }
                case 0x6D:
                    {
                        Load(ref L, L);
                        break;
                    }
                case 0x6E:
                    {
                        LoadFromMem(ref L, HL);
                        break;
                    }
                case 0x6F:
                    {
                        Load(ref L, A);
                        break;
                    }
                case 0x70:
                    {
                        // LD (HL),  ...
                        LoadToMem(HL, B);
                        break;
                    }
                case 0x71:
                    {
                        LoadToMem(HL, C);
                        break;
                    }

                case 0x72:
                    {
                        LoadToMem(HL, D);
                        break;
                    }

                case 0x73:
                    {
                        LoadToMem(HL, E);
                        break;
                    }
                case 0x74:
                    {
                        LoadToMem(HL, H);
                        break;
                    }
                case 0x75:
                    {
                        LoadToMem(HL, L);
                        break;
                    }
                case 0x76:
                    {
                        // HALT | 1 | 4
                        Halt();
                        break;
                    }
                case 0x77:
                    {
                        // LD (HL), A
                        LoadToMem(HL, A);
                        break;
                    }
                case 0x78:
                    {
                        // LD A, ...
                        Load(ref A, B);
                        break;
                    }
                case 0x79:
                    {
                        Load(ref A, C);
                        break;
                    }
                case 0x7A:
                    {
                        Load(ref A, D);
                        break;
                    }
                case 0x7B:
                    {
                        Load(ref A, E);
                        break;
                    }
                case 0x7C:
                    {
                        Load(ref A, H);
                        break;
                    }
                case 0x7D:
                    {
                        Load(ref A, L);
                        break;
                    }
                case 0x7E:
                    {
                        LoadFromMem(ref A, HL);
                        break;
                    }
                case 0x7F:
                    {
                        Load(ref A, A);
                        break;
                    }
                case 0x80:
                    {
                        // ADD A, ...
                        Add(ref A, B);
                        break;
                    }
                case 0x81:
                    {
                        Add(ref A, C);
                        break;
                    }

                case 0x82:
                    {
                        Add(ref A, D);
                        break;
                    }

                case 0x83:
                    {
                        Add(ref A, E);
                        break;
                    }
                case 0x84:
                    {
                        Add(ref A, H);
                        break;
                    }
                case 0x85:
                    {
                        Add(ref A, L);
                        break;
                    }
                case 0x86:
                    {
                        AddFromMem(ref A, HL);
                        break;
                    }
                case 0x87:
                    {
                        Add(ref A, A);
                        break;
                    }
                case 0x88:
                    {
                        // ADC A, ...
                        AddWithCarry(ref A, B);
                        break;
                    }
                case 0x89:
                    {
                        AddWithCarry(ref A, C);
                        break;
                    }
                case 0x8A:
                    {
                        AddWithCarry(ref A, D);
                        break;
                    }
                case 0x8B:
                    {
                        AddWithCarry(ref A, E);
                        break;
                    }
                case 0x8C:
                    {
                        AddWithCarry(ref A, H);
                        break;
                    }
                case 0x8D:
                    {
                        AddWithCarry(ref A, L);
                        break;
                    }
                case 0x8E:
                    {
                        AddWithCarry(ref A, Read(HL));
                        break;
                    }
                case 0x8F:
                    {
                        AddWithCarry(ref A, A);
                        break;
                    }
                case 0x90:
                    {
                        // SUB ...
                        Subtract(ref A, B);
                        break;
                    }
                case 0x91:
                    {
                        Subtract(ref A, C);
                        break;
                    }

                case 0x92:
                    {
                        Subtract(ref A, D);
                        break;
                    }

                case 0x93:
                    {
                        Subtract(ref A, E);
                        break;
                    }
                case 0x94:
                    {
                        Subtract(ref A, H);
                        break;
                    }
                case 0x95:
                    {
                        Subtract(ref A, L);
                        break;
                    }
                case 0x96:
                    {
                        SubtractFromMem(ref A, HL);
                        break;
                    }
                case 0x97:
                    {
                        Subtract(ref A, A);
                        break;
                    }
                case 0x98:
                    {
                        // SBC ..
                        SubtractWithCarry(ref A, B);
                        break;
                    }
                case 0x99:
                    {
                        SubtractWithCarry(ref A, C);
                        break;
                    }
                case 0x9A:
                    {
                        SubtractWithCarry(ref A, D);
                        break;
                    }
                case 0x9B:
                    {
                        SubtractWithCarry(ref A, E);
                        break;
                    }
                case 0x9C:
                    {
                        SubtractWithCarry(ref A, H);
                        break;
                    }
                case 0x9D:
                    {
                        SubtractWithCarry(ref A, L);
                        break;
                    }
                case 0x9E:
                    {
                        SubtractWithCarry(ref A, Read(HL));
                        break;
                    }
                case 0x9F:
                    {
                        SubtractWithCarry(ref A, A);
                        break;
                    }
                case 0xA0:
                    {
                        // AND ...
                        And(ref A, B);
                        break;
                    }
                case 0xA1:
                    {
                        And(ref A, C);
                        break;
                    }

                case 0xA2:
                    {
                        And(ref A, D);
                        break;
                    }

                case 0xA3:
                    {
                        And(ref A, E);
                        break;
                    }
                case 0xA4:
                    {
                        And(ref A, H);
                        break;
                    }
                case 0xA5:
                    {
                        And(ref A, F);
                        break;
                    }
                case 0xA6:
                    {

                        And(ref A, Read(HL));
                        break;
                    }
                case 0xA7:
                    {
                        And(ref A, A);
                        break;
                    }
                case 0xA8:
                    {
                        // XOR ...
                        Xor(ref A, B);
                        break;
                    }
                case 0xA9:
                    {
                        Xor(ref A, C);
                        break;
                    }
                case 0xAA:
                    {
                        Xor(ref A, D);
                        break;
                    }
                case 0xAB:
                    {
                        Xor(ref A, E);
                        break;
                    }
                case 0xAC:
                    {
                        Xor(ref A, H);
                        break;
                    }
                case 0xAD:
                    {
                        Xor(ref A, F);
                        break;
                    }
                case 0xAE:
                    {

                        Xor(ref A, Read(HL));
                        break;
                    }
                case 0xAF:
                    {
                        Xor(ref A, A);
                        break;
                    }
                case 0xB0:
                    {
                        // OR ...
                        Or(ref A, B);
                        break;
                    }
                case 0xB1:
                    {
                        Or(ref A, C);
                        break;
                    }

                case 0xB2:
                    {
                        Or(ref A, D);
                        break;
                    }

                case 0xB3:
                    {
                        Or(ref A, E);
                        break;
                    }
                case 0xB4:
                    {
                        Or(ref A, H);
                        break;
                    }
                case 0xB5:
                    {
                        Or(ref A, F);
                        break;
                    }
                case 0xB6:
                    {
                        Or(ref A, Read(HL));
                        break;
                    }
                case 0xB7:
                    {
                        Or(ref A, A);
                        break;
                    }
                case 0xB8:
                    {
                        // CP ...
                        Compare(A, B);
                        break;
                    }
                case 0xB9:
                    {
                        Compare(A, C);
                        break;
                    }
                case 0xBA:
                    {
                        Compare(A, D);
                        break;
                    }
                case 0xBB:
                    {
                        Compare(A, E);
                        break;
                    }
                case 0xBC:
                    {
                        Compare(A, H);
                        break;
                    }
                case 0xBD:
                    {
                        Compare(A, L);
                        break;
                    }
                case 0xBE:
                    {
                        Compare(A, Read(HL));
                        break;
                    }
                case 0xBF:
                    {
                        Compare(A, A);
                        break;
                    }
                case 0xC0:
                    {
                        // RET NZ | 1 | 20/8
                        if (!ZeroFlag) Return();
                        break;
                    }
                case 0xC1:
                    {
                        // POP BC | 1 | 12
                        Pop(ref B, ref C);
                        break;
                    }

                case 0xC2:
                    {
                        // JP NZ, a16
                        if (!ZeroFlag) JumpTo(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0xC3:
                    {
                        // JP a16
                        JumpTo(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xC4:
                    {
                        // CALL NZ, a16
                        if (!ZeroFlag) Call(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xC5:
                    {
                        // Push BC | 1 | 16
                        Push(B, C);
                        break;
                    }
                case 0xC6:
                    {
                        // ADD A, d8
                        Add(ref A, Fetch());
                        break;
                    }
                case 0xC7:
                    {
                        // RST 00H | 1 | 16
                        Restart(0x00);
                        break;
                    }
                case 0xC8:
                    {
                        // RET Z
                        if (ZeroFlag) Return();
                        break;
                    }
                case 0xC9:
                    {
                        // RET
                        Return();
                        break;
                    }
                case 0xCA:
                    {
                        // JP Z, a16
                        if (ZeroFlag) JumpTo(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xCB:
                    {
                        // I think the solution will be to use same switch but with a flag. To save lines...and my sanity
                        // PREFIX CB => Another set of instructions

                        break;
                    }
                case 0xCC:
                    {
                        // CALL Z, a16
                        if (ZeroFlag) Call(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xCD:
                    {
                        // CALL a16
                        Call(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xCE:
                    {
                        // ADC A, d8
                        AddWithCarry(ref A, Fetch());
                        break;
                    }
                case 0xCF:
                    {
                        // RST 08H
                        Restart(0x08);
                        break;
                    }
                case 0xD0:
                    {
                        if (!CarryFlag) Return();
                        break;
                    }
                case 0xD1:
                    {
                        Pop(ref D, ref E);
                        break;
                    }

                case 0xD2:
                    {
                        if (!CarryFlag) JumpTo(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }

                case 0xD3:
                    {
                        // EMPTY Maybe case a crash?
                        break;
                    }
                case 0xD4:
                    {
                        if (!CarryFlag) Call(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xD5:
                    {
                        Push(D, E);
                        break;
                    }
                case 0xD6:
                    {
                        Subtract(ref A, Fetch());
                        break;
                    }
                case 0xD7:
                    {
                        Restart(0x10);
                        break;
                    }
                case 0xD8:
                    {
                        if (CarryFlag) Return();
                        break;
                    }
                case 0xD9:
                    {
                        ReturnAndEnableInterrupt();
                        break;
                    }
                case 0xDA:
                    {
                        if (CarryFlag) JumpTo(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xDB:
                    {
                        //EMpty
                        break;
                    }
                case 0xDC:
                    {
                        if (CarryFlag) Call(ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xDD:
                    {
                        // Empty
                        break;
                    }
                case 0xDE:
                    {
                        SubtractWithCarry(ref A, Fetch());
                        break;
                    }
                case 0xDF:
                    {
                        Restart(0x18);
                        break;
                    }
                case 0xE0:
                    {
                        // LDH (a8), A
                        LoadToMem((ushort)(0xFF00 + Fetch()), A);
                        break;
                    }
                case 0xE1:
                    {
                        Pop(ref H, ref L);
                        break;
                    }

                case 0xE2:
                    {
                        LoadToMem((ushort)(0xFF00 + C), A);
                        break;
                    }

                case 0xE3:
                    {
                        //Empty
                        break;
                    }
                case 0xE4:
                    {
                        //Empty
                        break;
                    }
                case 0xE5:
                    {
                        Push(H, L);
                        break;
                    }
                case 0xE6:
                    {
                        And(ref A, Fetch());
                        break;
                    }
                case 0xE7:
                    {
                        Restart(0x20);
                        break;
                    }
                case 0xE8:
                    {
                        AddToStackPointer((sbyte)Fetch());
                        break;
                    }
                case 0xE9:
                    {
                        JumpTo(HL);
                        break;
                    }
                case 0xEA:
                    {
                        LoadToMem(ConcatBytes(Fetch(), Fetch()), A);
                        break;
                    }
                case 0xEB:
                    {
                        // EMpty
                        break;
                    }
                case 0xEC:
                    {
                        // Empty
                        break;
                    }
                case 0xED:
                    {
                        // Empty
                        break;
                    }
                case 0xEE:
                    {
                        Xor(ref A, Fetch());
                        break;
                    }
                case 0xEF:
                    {
                        Restart(0x28);
                        break;
                    }
                case 0xF0:
                    {
                        LoadFromMem(ref A, (ushort)(0xFF00 + Fetch()));
                        break;
                    }
                case 0xF1:
                    {
                        Pop(ref A, ref F);
                        break;
                    }

                case 0xF2:
                    {
                        LoadFromMem(ref A, (ushort)(0xFF00 + C));
                        break;
                    }

                case 0xF3:
                    {
                        // DI | 1 | 4 (Disable Interrupts)
                        DisableInterrupt();
                        break;
                    }
                case 0xF4:
                    {
                        //Empty
                        break;
                    }
                case 0xF5:
                    {
                        Push(A, F);
                        break;
                    }
                case 0xF6:
                    {
                        Or(ref A, Fetch());
                        break;
                    }
                case 0xF7:
                    {
                        Restart(0x30);
                        break;
                    }
                case 0xF8:
                    {
                        // LD HL, SP + r8
                        sbyte r8 = (sbyte)Fetch();
                        ushort prevSP = SP;
                        AddToStackPointer(r8);
                        Load(ref H, ref L, SP);
                        //Hack to set flags the same way as "AddToStackPointer" but without adding to SP
                        SP = prevSP;
                        break;
                    }
                case 0xF9:
                    {
                        // LD SP, HL
                        Load(ref SP, HL);
                        break;
                    }
                case 0xFA:
                    {
                        LoadFromMem(ref A, ConcatBytes(Fetch(), Fetch()));
                        break;
                    }
                case 0xFB:
                    {
                        // EI (enable interrupts)
                        EnableInterrupt();
                        break;
                    }
                case 0xFC:
                    {
                        // Empty
                        break;
                    }
                case 0xFD:
                    {
                        // Empty
                        break;
                    }
                case 0xFE:
                    {
                        Compare(A, Fetch());
                        break;
                    }
                case 0xFF:
                    {
                        Restart(0x38);
                        break;
                    }
            }
    */
}