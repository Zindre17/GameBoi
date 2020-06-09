using System;

class CPU
{
    private Bus bus;

    private bool isHalted = false;
    private bool isInterruptEnabled = true;
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

    private ushort SP; //stack pointer

    #endregion


    #region Flags

    const byte zeroBitMask = (1 << 7);
    const byte subtractBitMask = (1 << 6);
    const byte halfCarryBitMask = (1 << 5);
    const byte carryBitMask = (1 << 4);
    public bool ZeroFlag => (F & zeroBitMask) == zeroBitMask;
    public bool SubtractFlag => (F & subtractBitMask) == subtractBitMask;
    public bool HalfCarryFlag => (F & halfCarryBitMask) == halfCarryBitMask;
    public bool CarryFlag => (F & carryBitMask) == carryBitMask;

    public enum Flag
    {
        Zero, Subtract, HalfCarry, Carry
    }
    public void SetFlag(Flag flag, bool on)
    {
        byte mask;
        switch (flag)
        {
            case Flag.Zero:
                {
                    mask = zeroBitMask;
                    break;
                }
            case Flag.Subtract:
                {
                    mask = subtractBitMask;
                    break;
                }
            case Flag.HalfCarry:
                {
                    mask = halfCarryBitMask;
                    break;
                }
            case Flag.Carry:
                {
                    mask = carryBitMask;
                    break;
                }
            default: throw new Exception("Not valid flag");
        }

        if (on)
            F |= mask;
        else
        {
            byte flippedMask = (byte)(mask ^ 0xff);
            F &= flippedMask;
        }

    }

    #endregion



    #region Instructions

    // 0x00
    //4 cycles 
    private void NOP() { }
    private void STOP(byte arg) { }
    private void HALT()
    {
        // Halts CPU until interrupt happens => Perform NOPs meanwhile to not fuck up memory
        isHalted = true;
    }

    private void DI() { }
    private void EI() { }

    private void PREFIX_CB() { }


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
        bus.Write(address, source);
    }

    private void LoadToMem(ushort address, ushort source)
    {
        bus.Write(address, GetLowByte(source));
        bus.Write(address++, GetHighByte(source));
    }

    private void LoadFromMem(ref byte target, ushort address)
    {
        if (bus.Read(address, out byte value))
            target = value;
        else throw new Exception("Failed to load byte form memory");
    }

    private void Add(ref byte target, byte operand)
    {
        SetFlag(Flag.HalfCarry, IsHalfCarryOnAddition(target, operand));
        int result = target + operand;
        SetFlag(Flag.Carry, IsCarryOnAddition(result));
        target = (byte)result;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, false);
    }

    private void Add(ref byte targetHigh, ref byte targetLow, byte operandHigh, byte operandLow)
    {
        int resultLow = targetLow + operandLow;
        int carryLow = IsCarryOnAddition(resultLow) ? 1 : 0;
        int resultHigh = targetHigh + operandHigh + carryLow;
        bool isCarryHigh = IsCarryOnAddition(resultHigh);
        SetFlag(Flag.Carry, isCarryHigh);
        SetFlag(Flag.Subtract, false);
        byte highAddition = (byte)(carryLow + operandHigh);
        SetFlag(Flag.HalfCarry, IsHalfCarryOnAddition(targetHigh, highAddition));
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
        bus.Read(address, out byte value);
        Add(ref target, value);
    }

    private void AddWithCarry(ref byte target, byte operand)
    {
        byte newOperand = (byte)(operand + (CarryFlag ? 1 : 0));
        Add(ref target, newOperand);
    }

    private void Subtract(ref byte target, byte operand)
    {
        SetFlag(Flag.Subtract, true);
        int result = target - operand;
        SetFlag(Flag.Zero, result == 0);
        SetFlag(Flag.Carry, result < 0);
        SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(target, operand));
        target = (byte)result;
    }

    private void SubtractFromMem(ref byte target, ushort address)
    {
        if (bus.Read(address, out byte value))
            Subtract(ref target, value);
        else
            throw new MemoryReadException(address);
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
        SetFlag(Flag.HalfCarry, IsHalfCarryOnAddition(target, 1)); // set if carry from bit 3
        target++;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, false);
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
        bus.Read(address, out byte value);
        Increment(ref value);
        bus.Write(address, value);
    }

    private void Decrement(ref byte target)
    {
        SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(target, 1)); // set if borrow from bit 4
        target--;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, true);
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
        bus.Read(address, out byte value);
        Decrement(ref value);
        bus.Write(address, value);
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
        SetFlag(Flag.Zero, false);
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, isCarry);
        if (isCarry)
            target = (byte)(rotated | 1); //wrap around carry bit
        else
            target = (byte)rotated; //no need for wrap around
    }

    private void RotateRightWithCarry(ref byte target)
    {
        bool isCarry = (target & 1) != 0;
        int rotated = target >> 1;
        SetFlag(Flag.Zero, false);
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, isCarry);
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
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, isCarry);
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
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, isCarry);
        if (oldIsCarry)
            target = (byte)(rotated | 0x80);
        else
            target = (byte)rotated;
    }

    private void JumpBy(byte increment) //actually signed
    {
        PC = (byte)(PC + (sbyte)increment);
        PC += 2;
    }

    private void JumpTo(ushort newPC)
    {
        PC = newPC;
    }

    private void Complement(ref byte target)
    {
        SetFlag(Flag.HalfCarry, true);
        SetFlag(Flag.Subtract, true);
        target ^= 0xFF;
    }

    private void And(ref byte target, byte operand)
    {
        target &= operand;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, true);
        SetFlag(Flag.Carry, false);
    }

    private void Xor(ref byte target, byte operand)
    {
        target ^= operand;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, false);
    }

    private void Or(ref byte target, byte operand)
    {
        target |= operand;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, false);
        SetFlag(Flag.HalfCarry, false);
        SetFlag(Flag.Carry, false);
    }

    private void Compare(byte target, byte operand)
    {
        int result = target - operand;
        SetFlag(Flag.Zero, result == 0);
        SetFlag(Flag.Subtract, true);
        SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(target, operand));
        SetFlag(Flag.Carry, result < 0);
    }

    private void Return()
    {
        if (bus.Read(SP++, out byte newPCLow) && bus.Read(SP++, out byte newPCHigh))
        {
            PC = ConcatBytes(newPCHigh, newPCLow);
        }
    }

    private void ReturnFromInterrupt()
    {
        //TODO
    }

    private void ReturnFromNonMaskableInterrupt()
    {
        //TODO
    }

    private void Push(byte high, byte low)
    {
        if (!bus.Write(--SP, high))
            throw new MemoryWriteException(++SP);
        if (!bus.Write(--SP, low))
            throw new MemoryWriteException(++SP);

    }

    private void Pop(ref byte targetHigh, ref byte targetLow)
    {
        if (bus.Read(SP++, out byte sourceLow))
            targetLow = sourceLow;
        else
            throw new MemoryReadException(--SP);

        if (bus.Read(SP++, out byte sourceHigh))
            targetHigh = sourceHigh;
        else
            throw new MemoryReadException(--SP);
    }

    private void Call(ushort address)
    {
        bus.Write(--SP, GetHighByte(PC));
        bus.Write(--SP, GetLowByte(PC));
        PC = address;
    }

    private void RST(byte newPC)
    {
        if (!bus.Write(--SP, GetHighByte(PC)))
            throw new MemoryWriteException(++SP);

        if (!bus.Write(--SP, GetLowByte(PC)))
            throw new MemoryWriteException(++SP);

        PC = newPC;
    }

    private void AddToStackPointer(sbyte operand)
    {
        int result = (SP + operand);
        SP = (ushort)result;
        SetFlag(Flag.Zero, false);
        SetFlag(Flag.Subtract, false);
        if (operand < 0)
        {
            SetFlag(Flag.Carry, IsCarryOnSubtraction(result));
            SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(GetLowByte(SP), (byte)operand));
        }
        else
        {
            SetFlag(Flag.Carry, IsCarryOnAddition(result));
            SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(GetLowByte(SP), (byte)operand));
        }
    }
    #endregion

    public void PerformInstruction(byte opCode, byte arg1 = 0, byte arg2 = 0)
    {
        switch (opCode)
        {
            //intruction format: Name bytes cycles

            case 0x00:
                {
                    //NOP | 1 | 4
                    NOP();
                    break;
                }

            case 0x01:
                {
                    // LD BC, d16 | 3 | 12
                    Load(ref B, ref C, ConcatBytes(arg1, arg2));
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
                    Load(ref B, arg1);
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
                    ushort address = ConcatBytes(arg1, arg2);
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
                    Load(ref C, arg1);
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
                    STOP(arg1);
                    break;
                }
            case 0x11:
                {
                    // LD DE, d16 | 3 | 12 
                    Load(ref D, ref E, ConcatBytes(arg1, arg2));
                    break;
                }

            case 0x12:
                {
                    // LD (DE), A | 3 | 12
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
                    Load(ref D, arg1);
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
                    JumpBy(arg1);
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
                    Load(ref E, arg1);
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
                        JumpBy(arg1);
                    break;
                }
            case 0x21:
                {
                    // LD HL, d16 | 3 | 12 
                    Load(ref H, ref L, ConcatBytes(arg1, arg2));
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
                    Load(ref H, arg1);
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
                    if (ZeroFlag) JumpBy(arg1);
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
                    Load(ref L, arg1);
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
                    if (!CarryFlag) JumpBy(arg1);
                    break;
                }
            case 0x31:
                {
                    // LD SP, d16
                    Load(ref SP, ConcatBytes(arg1, arg2));
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
                    LoadToMem(HL, arg1);
                    break;
                }
            case 0x37:
                {
                    // SCF | 1 | 4 (Set Carry Flag)
                    SetFlag(Flag.Carry, true);
                    SetFlag(Flag.HalfCarry, false);
                    SetFlag(Flag.Subtract, false);
                    break;
                }
            case 0x38:
                {
                    // JR C, r8
                    if (CarryFlag) JumpBy(arg1);
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
                    if (bus.Read(HL, out byte value))
                        Load(ref A, value);
                    else
                        throw new MemoryReadException(HL);

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
                    Load(ref A, arg1);
                    break;
                }
            case 0x3F:
                {
                    // CCF
                    SetFlag(Flag.Carry, !CarryFlag);
                    SetFlag(Flag.HalfCarry, false);
                    SetFlag(Flag.Subtract, false);
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
                    HALT();
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
                    if (bus.Read(HL, out byte value))
                        AddWithCarry(ref A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (bus.Read(HL, out byte value))
                        SubtractWithCarry(ref A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (bus.Read(HL, out byte value))
                        And(ref A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (bus.Read(HL, out byte value))
                        Xor(ref A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (bus.Read(HL, out byte value))
                        Or(ref A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (bus.Read(HL, out byte value))
                        Compare(A, value);
                    else
                        throw new MemoryReadException(HL);
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
                    if (!ZeroFlag) JumpTo(ConcatBytes(arg1, arg2));
                    break;
                }

            case 0xC3:
                {
                    // JP a16
                    JumpTo(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xC4:
                {
                    // CALL NZ, a16
                    Call(ConcatBytes(arg1, arg2));
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
                    Add(ref A, arg1);
                    break;
                }
            case 0xC7:
                {
                    // RST 00H | 1 | 16
                    RST(0x00);
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
                    if (ZeroFlag) JumpTo(ConcatBytes(arg1, arg2));
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
                    if (ZeroFlag) Call(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xCD:
                {
                    // CALL a16
                    Call(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xCE:
                {
                    // ADC A, d8
                    AddWithCarry(ref A, arg1);
                    break;
                }
            case 0xCF:
                {
                    // RST 08H
                    RST(0x08);
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
                    if (!CarryFlag) JumpTo(ConcatBytes(arg1, arg2));
                    break;
                }

            case 0xD3:
                {
                    // EMPTY Maybe case a crash?
                    break;
                }
            case 0xD4:
                {
                    if (!CarryFlag) Call(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xD5:
                {
                    Push(D, E);
                    break;
                }
            case 0xD6:
                {
                    Subtract(ref A, arg1);
                    break;
                }
            case 0xD7:
                {
                    RST(0x10);
                    break;
                }
            case 0xD8:
                {
                    if (CarryFlag) Return();
                    break;
                }
            case 0xD9:
                {
                    ReturnFromInterrupt();
                    break;
                }
            case 0xDA:
                {
                    if (CarryFlag) JumpTo(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xDB:
                {
                    //EMpty
                    break;
                }
            case 0xDC:
                {
                    if (CarryFlag) Call(ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xDD:
                {
                    // Empty
                    break;
                }
            case 0xDE:
                {
                    SubtractWithCarry(ref A, arg1);
                    break;
                }
            case 0xDF:
                {
                    RST(0x18);
                    break;
                }
            case 0xE0:
                {
                    // LDH (a8), A
                    LoadToMem((ushort)(0xFF00 + arg1), A);
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
                    And(ref A, arg1);
                    break;
                }
            case 0xE7:
                {
                    RST(0x20);
                    break;
                }
            case 0xE8:
                {
                    AddToStackPointer((sbyte)arg1);
                    break;
                }
            case 0xE9:
                {
                    JumpTo(HL);
                    break;
                }
            case 0xEA:
                {
                    LoadToMem(ConcatBytes(arg1, arg2), A);
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
                    Xor(ref A, arg1);
                    break;
                }
            case 0xEF:
                {
                    RST(0x28);
                    break;
                }
            case 0xF0:
                {
                    LoadFromMem(ref A, (ushort)(0xFF00 + arg1));
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
                    // DI | 1 | 4 (Disable Interupts)
                    isInterruptEnabled = false;
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
                    Or(ref A, arg1);
                    break;
                }
            case 0xF7:
                {
                    RST(0x30);
                    break;
                }
            case 0xF8:
                {
                    // LD HL, SP + r8
                    sbyte r8 = (sbyte)arg1;
                    int tempValue = SP + r8;
                    SetFlag(Flag.Zero, false);
                    SetFlag(Flag.Subtract, false);
                    if (r8 < 0)
                    {
                        SetFlag(Flag.Carry, IsCarryOnSubtraction(tempValue));
                        SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(GetLowByte(SP), (byte)r8));
                    }
                    else
                    {
                        SetFlag(Flag.Carry, IsCarryOnAddition(tempValue));
                        SetFlag(Flag.HalfCarry, IsHalfCarryOnAddition(GetLowByte(SP), arg1));
                    }
                    Load(ref H, ref L, (ushort)tempValue);
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
                    LoadFromMem(ref A, ConcatBytes(arg1, arg2));
                    break;
                }
            case 0xFB:
                {
                    // EI (enable interrupts)
                    isInterruptEnabled = true;
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
                    Compare(A, arg1);
                    break;
                }
            case 0xFF:
                {
                    RST(0x38);
                    break;
                }
        }
    }

    // source http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
}

class MemoryReadException : Exception
{
    public MemoryReadException(ushort address) : base($"Failed to read from memory. Address: {address}") { }
}

class MemoryWriteException : Exception
{
    public MemoryWriteException(ushort address) : base($"Failed to write to memory. Address: {address}") { }
}

