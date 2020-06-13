using System;

class CPU : Hardware<IBus>
{
    private bool IME = true; // Interrupt Master Enable
    private const ushort IE_address = 0xFFFF;
    private const ushort IF_address = 0xFF0F;

    private bool isHalted = false;


    #region Registers

    private byte A; // accumulator
    private byte F; // flag register

    private byte B;
    private byte C;

    private byte D;
    private byte E;

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

    const byte zero_mask = (1 << 7);
    const byte subtract_mask = (1 << 6);
    const byte halfCarry_mask = (1 << 5);
    const byte carry_mask = (1 << 4);
    public bool ZeroFlag => (F & zero_mask) == zero_mask;
    public bool SubtractFlag => (F & subtract_mask) == subtract_mask;
    public bool HalfCarryFlag => (F & halfCarry_mask) == halfCarry_mask;
    public bool CarryFlag => (F & carry_mask) == carry_mask;

    private void SetFlags(bool Z, bool S, bool H, bool C)
    {
        SetZeroFlag(Z);
        SetSubtractFlag(S);
        SetHalfCarryFlag(H);
        SetCarryFlag(C);
    }

    private void SetZeroFlag(bool Z) => SetFlag(zero_mask, Z);
    private void SetSubtractFlag(bool S) => SetFlag(subtract_mask, S);
    private void SetHalfCarryFlag(bool H) => SetFlag(halfCarry_mask, H);
    private void SetCarryFlag(bool C) => SetFlag(carry_mask, C);


    private void SetFlag(byte mask, bool on)
    {
        if (on) F |= mask;
        else F &= Invert(mask);
    }

    private byte Invert(byte b)
    {
        return (byte)(b ^ 0xFF);
    }

    #endregion

    long clock = 0;

    public void Tick()
    {
        //increase clock
        clock++;

        if (isHalted)
        {
            NoOperation();
            return;
        }

        //---------------- Standard procedure ----------------

        byte opCode = Fetch();
        PerformInstruction(opCode);

        //----------------------------------------------------

        //---------------- Handle Interrupts -----------------

        HandleInterrupts();

        //----------------------------------------------------
    }

    private const byte V_Blank_mask = (1 << 0);
    private const byte LCDC_mask = (1 << 1);
    private const byte Timer_mask = (1 << 2);
    private const byte Serial_mask = (1 << 3);
    private const byte Hi_Lo_mask = (1 << 4);

    private void HandleInterrupts()
    {
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

        if (ShouldInterrupt(IE, IF, V_Blank_mask))
        {
            Interrupt(0x0040, V_Blank_mask, IF);
        }
        else if (ShouldInterrupt(IE, IF, LCDC_mask))
        {
            Interrupt(0x0048, LCDC_mask, IF);
        }
        else if (ShouldInterrupt(IE, IF, Timer_mask))
        {
            Interrupt(0x0050, Timer_mask, IF);
        }
        else if (ShouldInterrupt(IE, IF, Serial_mask))
        {
            Interrupt(0x0058, Serial_mask, IF);
        }
        else if (ShouldInterrupt(IE, IF, Hi_Lo_mask))
        {
            Interrupt(0x0060, Hi_Lo_mask, IF);
        }
    }

    private void Interrupt(ushort startingAddress, byte flag, byte IF)
    {
        IME = false;
        Write(IF_address, (byte)(IF ^ flag)); // remove the interrupt request that is granted 
        Push(PC_P, PC_C);
        JumpTo(startingAddress);
    }

    private bool ShouldInterrupt(byte IE, byte IF, byte mask)
    {
        return (IE & mask) == mask && (IF & mask) == mask;
    }

    private byte Fetch()
    {
        //Fetch instruction and increment PC after
        return Read(PC++);
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

    #region Instructions

    // 0x00
    //4 cycles 
    private void NoOperation() { }
    private void Stop(byte arg) { }
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

    private void PREFIX_CB()
    {
        PerformInstruction(Fetch(), true); //TODO: ensure this is correct
    }


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

    private void LoadFromMem(ref byte target, ushort address)
    {
        target = Read(address);
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

    private void AddFromMem(ref byte target, ushort address)
    {
        Add(ref target, Read(address));
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

    private void SubtractFromMem(ref byte target, ushort address)
    {
        Subtract(ref target, Read(address));
    }

    private void SubtractWithCarry(ref byte target, byte operand)
    {
        byte newOperand = (byte)(operand + (CarryFlag ? 1 : 0));
        Subtract(ref target, newOperand);
    }

    private bool IsHalfCarryOnAddition(byte target, byte operand)
    {
        int target4 = target & 0x0F;
        int operand4 = operand & 0x0F;
        return ((operand4 + target4) & 0x10) == 0x10;
    }

    private bool IsCarryOnAddition(int result)
    {
        return result > 0x00FF;
    }

    private bool IsHalfCarryOnSubtraction(byte target, byte operand)
    {
        int target4 = target & 0x0F;
        int operand4 = operand & 0x0F;
        int result = target4 - operand4;
        return result < 0;
    }

    private bool IsCarryOnSubtraction(int result)
    {
        return result < 0;
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

    private ushort ConcatBytes(byte h, byte l)
    {
        return (ushort)((h << 8) | l);
    }

    private byte GetLowByte(ushort doubleWord)
    {
        return (byte)doubleWord;
    }

    private byte GetHighByte(ushort doubleWord)
    {
        return (byte)(doubleWord >> 8);
    }


    private void RotateLeftWithCarry(ref byte target)
    {
        int rotated = target << 1;
        bool isCarry = IsCarryOnAddition(rotated);
        SetFlags(false, false, false, isCarry);
        if (isCarry)
            target = (byte)(rotated | 1); //wrap around carry bit
        else
            target = (byte)rotated; //no need for wrap around
    }

    private void RotateRightWithCarry(ref byte target)
    {
        bool isCarry = (target & 1) != 0;
        int rotated = target >> 1;
        SetFlags(false, false, false, isCarry);
        if (isCarry)
            target = (byte)(rotated | 0x80);
        else
            target = (byte)rotated;
    }

    private void RotateLeft(ref byte target)
    {
        int rotated = target << 1;
        bool oldIsCarry = CarryFlag;
        bool isCarry = IsCarryOnAddition(rotated);
        // Some conflicting  documentation on flagging from this instruction....
        //SetFlag(Flag.Zero, false);
        SetSubtractFlag(false);
        SetHalfCarryFlag(false);
        SetCarryFlag(isCarry);
        if (oldIsCarry)
            target = (byte)(rotated | 1);
        else
            target = (byte)rotated;
    }

    private void RotateRight(ref byte target)
    {
        bool oldIsCarry = CarryFlag;
        bool isCarry = (target & 1) != 0;
        int rotated = target >> 1;
        // Some conflicting  documentation on flagging from this instruction....
        //SetFlag(Flag.Zero, false);
        SetSubtractFlag(false);
        SetHalfCarryFlag(false);
        SetCarryFlag(isCarry);
        if (oldIsCarry)
            target = (byte)(rotated | 0x80);
        else
            target = (byte)rotated;
    }

    private void JumpBy(byte increment) //actually signed
    {
        PC = (byte)(PC + (sbyte)increment);
    }

    private void JumpTo(ushort newPC)
    {
        PC = newPC;
    }

    private void Complement(ref byte target)
    {
        SetHalfCarryFlag(true);
        SetSubtractFlag(true);
        target ^= 0xFF;
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

    public void PerformInstruction(byte opCode, bool CB_mode = false)
    {
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
                    SetCarryFlag(true);
                    SetHalfCarryFlag(false);
                    SetSubtractFlag(false);
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
                    SetSubtractFlag(false);
                    SetHalfCarryFlag(CarryFlag); // Z80 doc says copy carry flag, gameboy-doc says reset...
                    SetCarryFlag(!CarryFlag);
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
                    Call(ConcatBytes(Fetch(), Fetch()));
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
    }

    // source http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
}


