using System;

class CPU
{
    private Bus bus;

    #region Registers

    private byte A; // accumulator
    private byte F; // flag register

    private byte B;
    private byte C;

    private byte D;
    private byte E;

    private byte H;
    private byte L;

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
    private void HALT() { }

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


    private void Decrement(ref byte target)
    {
        SetFlag(Flag.HalfCarry, IsHalfCarryOnSubtraction(target, 1)); // set if borrow from bit 4
        target--;
        SetFlag(Flag.Zero, target == 0);
        SetFlag(Flag.Subtract, true);
    }
    private void Decrement(ref byte targetHigh, ref byte targetLow)
    {

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

    private void Jump(byte increment)
    {
        PC += increment;
    }

    private void Complement(ref byte target)
    {
        SetFlag(Flag.HalfCarry, true);
        SetFlag(Flag.Subtract, true);
        target ^= 0xFF;
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
                    Jump(arg1);
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
                        Jump(arg1);
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
                    LoadToMem(ConcatBytes(H, L), A);
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
                    if (ZeroFlag) Jump(arg1);
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
                    LoadFromMem(ref A, ConcatBytes(H, L));
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

        }
    }

    // source http://marc.rawer.de/Gameboy/Docs/GBCPUman.pdf
}