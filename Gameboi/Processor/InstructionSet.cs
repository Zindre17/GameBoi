using System;
using Gameboi.Extensions;

namespace Gameboi.Processor;

public class InstructionSet
{
    private readonly SystemState state;
    private readonly Bus bus;

    public InstructionSet(SystemState state, Bus bus)
    {
        this.state = state;
        this.bus = bus;
    }

    private byte ReadNextInstructionByte()
    {
        return bus.Read(state.ProgramCounter++);
    }

    private ushort ReadImmediateAddress()
    {
        var low = ReadNextInstructionByte();
        return ReadNextInstructionByte().Concat(low);
    }

    private byte ReadImmediateByte()
    {
        return ReadNextInstructionByte();
    }

    public byte ExecuteInstruction(byte opCode)
    {
        if (state.IsHalted)
        {
            return NopDuration;
        }

        if (state.IsInCbMode)
        {
            return ExecuteCbInstruction(opCode);
        }

        return opCode switch
        {
            NopCode => NopDuration,

            StopCode => Stop(),
            HaltCode => Halt(),
            PrefixCbCode => PrefixCb(),
            DisableInterruptCode => DisableInterrupt(),
            EnableInterruptCode => EnableInterrupt(),

            DaaInstruction => Daa(),
            SetCarryFlagInstruction => SetCarryFlag(),
            ComplementInstruction => Complement(),
            ComplementCarryFlagInstruction => ComplementCarryFlag(),
            AddToStackPointerInstruction => AddToStackPointer(),

            var code when IsLoad8Operation(code) => Load8(code),
            var code when IsLoad16Operation(code) => Load16(code),

            var code when IsPopOperation(code) => Pop(code),
            var code when IsPushOperation(code) => Push(code),

            var code when IsIncrement8Operation(code) => Increment8(code),
            var code when IsIncrement16Operation(code) => Increment16(code),

            var code when IsDecrement8Operation(code) => Decrement8(code),
            var code when IsDecrement16Operation(code) => Decrement16(code),

            var code when IsRotateOperation(code) => Rotate(code),

            var code when IsRestartOperation(code) => Restart(code),

            var code when IsReturnOperation(code) => Return(code),
            var code when IsConditionalReturnOperation(code) => ConditionalReturn(code),

            var code when IsCallOperation(code) => Call(code),

            var code when IsRelativeJumpOperation(code) => RelativeJump(code),
            var code when IsExactJumpOperation(code) => JumpTo(code),

            var code when IsLogicalOperation(code) => ExecuteLogic(code),
            var code when IsAdd16Operation(code) => Add16(code),
            _ => 0
        };
    }

    private byte Stop()
    {
        // TODO: emulate stop/halt bug.
        state.IsHalted = true;
        ReadImmediateByte();
        return StopDuration;
    }

    private byte Halt()
    {
        // TODO: emulate stop/halt bug.
        state.IsHalted = true;
        return HaltDuration;
    }

    private byte PrefixCb()
    {
        state.IsInCbMode = true;
        return PrefixCbDuration;
    }

    private byte DisableInterrupt()
    {
        state.InterruptMasterEnable = false;
        return DisableInterruptDuration;
    }

    private byte EnableInterrupt()
    {
        state.InterruptMasterEnable = true;
        return EnableInterruptDuration;
    }

    private byte Daa()
    {
        var currentFlags = new CpuFlagRegister(state.Flags);
        var setCarry = currentFlags.IsSet(CpuFlags.Carry);

        ref var acc = ref state.Accumulator;

        if (currentFlags.IsSet(CpuFlags.Subtract))
        {
            if (setCarry)
            {
                acc -= 0x60;
            }
            if (currentFlags.IsSet(CpuFlags.HalfCarry))
            {
                acc -= 0x6;
            }
        }
        else
        {
            if (setCarry || acc > 0x99)
            {
                acc += 0x60;
                setCarry = true;
            }
            if (currentFlags.IsSet(CpuFlags.HalfCarry) || (acc & 0xF) > 0x09)
            {
                acc += 0x6;
            }
        }

        state.Flags = currentFlags
            .SetTo(CpuFlags.Carry, setCarry)
            .SetTo(CpuFlags.Zero, acc is 0)
            .Unset(CpuFlags.HalfCarry);

        return DaaDuration;
    }

    private byte SetCarryFlag()
    {
        state.Flags = new CpuFlagRegister(state.Flags)
            .Set(CpuFlags.Carry)
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry);

        return SetCarryFlagDuration;
    }

    private byte Complement()
    {
        state.Flags = new CpuFlagRegister(state.Flags)
            .Set(CpuFlags.Subtract | CpuFlags.HalfCarry);

        state.Accumulator = (byte)~state.Accumulator;
        return ComplementDuration;
    }

    private byte ComplementCarryFlag()
    {
        state.Flags = new CpuFlagRegister(state.Flags)
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .Flip(CpuFlags.Carry);

        return ComplementCarryFlagDuration;
    }

    private byte AddToStackPointer()
    {
        var immediateValue = ReadNextInstructionByte();

        state.Flags = new CpuFlagRegister(state.Flags)
            .Unset(CpuFlags.Zero | CpuFlags.Subtract)
            .SetTo(CpuFlags.HalfCarry, IsHalfCarryAdd(state.StackPointer, immediateValue))
            .SetTo(CpuFlags.Carry, IsCarryAdd(state.StackPointer, immediateValue));

        state.StackPointer = (ushort)(state.StackPointer + (sbyte)immediateValue);
        return AddToStackPointerDuration;
    }

    private static bool IsHalfCarryAdd(int a, int b)
    {
        return (a & 0xf) + (b & 0xf) > 0xf;
    }

    private static bool IsCarryAdd(int a, int b)
    {
        return a + b > 0xff;
    }


    private static bool IsLoad8Operation(byte opCode)
    {
        var highNibble = opCode >> 4;
        var lowNibble = opCode & 0x0f;
        return opCode switch
        {

            _ when highNibble < 4 && lowNibble is 2 or 6 or 0xa or 0xe => true, // 0x[0-3](2|6|a|e)
            > 0x3f and < 0x80 => true,
            0xe0 or 0xe2 or 0xea => true,
            0xf0 or 0xf2 or 0xfa => true,
            _ => false
        };
    }

    private byte Load8(byte opCode)
    {
        if (opCode is 0x02)
        {
            bus.Write(state.BC, state.Accumulator);
            return 8;
        }
        if (opCode is 0x12)
        {
            bus.Write(state.DE, state.Accumulator);
            return 8;
        }
        if (opCode is 0x22)
        {
            bus.Write(state.HL, state.Accumulator);
            if (state.Low++ is 0xff)
            {
                state.High++;
            }
            return 8;
        }
        if (opCode is 0x32)
        {
            bus.Write(state.HL, state.Accumulator);
            if (state.Low-- is 0)
            {
                state.High--;
            }
            return 8;
        }

        if (opCode is 0x0a)
        {
            state.Accumulator = bus.Read(state.BC);
            return 8;
        }
        if (opCode is 0x1a)
        {
            state.Accumulator = bus.Read(state.DE);
            return 8;
        }
        if (opCode is 0x2a)
        {
            state.Accumulator = bus.Read(state.HL);
            if (state.Low++ is 0xff)
            {
                state.High++;
            }
            return 8;
        }
        if (opCode is 0x3a)
        {
            state.Accumulator = bus.Read(state.HL);
            if (state.Low-- is 0)
            {
                state.High--;
            }
            return 8;
        }

        if (opCode is 0x06)
        {
            state.B = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x0e)
        {
            state.C = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x16)
        {
            state.D = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x1e)
        {
            state.E = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x26)
        {
            state.High = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x2e)
        {
            state.Low = ReadImmediateByte();
            return 8;
        }
        if (opCode is 0x36)
        {
            bus.Write(state.HL, ReadImmediateByte());
            return 12;
        }
        if (opCode is 0x3e)
        {
            state.Accumulator = ReadImmediateByte();
            return 8;
        }

        if (opCode is 0xe0)
        {
            bus.Write((ushort)(0xff00 + ReadImmediateByte()), state.Accumulator);
            return 12;
        }
        if (opCode is 0xf0)
        {
            state.Accumulator = bus.Read((ushort)(0xff00 + ReadImmediateByte()));
            return 12;
        }

        if (opCode is 0xe2)
        {
            bus.Write((ushort)(0xff00 + state.C), state.Accumulator);
            return 8;
        }
        if (opCode is 0xf2)
        {
            state.Accumulator = bus.Read((ushort)(0xff00 + state.C));
            return 8;
        }

        if (opCode is 0xea)
        {
            bus.Write(ReadImmediateAddress(), state.Accumulator);
            return 16;
        }
        if (opCode is 0xfa)
        {
            state.Accumulator = bus.Read(ReadImmediateAddress());
            return 16;
        }

        var value = GetSourceValue(opCode, out var didReadFromMemory);
        var duration = didReadFromMemory ? 8 : 4;

        switch (opCode)
        {
            case < 0x48:
                state.B = value;
                break;
            case < 0x50:
                state.C = value;
                break;
            case < 0x58:
                state.D = value;
                break;
            case < 0x60:
                state.E = value;
                break;
            case < 0x68:
                state.High = value;
                break;
            case < 0x70:
                state.Low = value;
                break;
            case < 0x78:
                bus.Write(state.HL, value);
                duration += 4;
                break;
            case < 0x80:
                state.Accumulator = value;
                break;
        }

        return (byte)duration;
    }

    private byte GetSourceValue(byte opCode, out bool didReadFromMemory)
    {
        var moddedOpCode = opCode % 8;

        didReadFromMemory = moddedOpCode is 6;
        return moddedOpCode switch
        {
            0 => state.B,
            1 => state.C,
            2 => state.D,
            3 => state.E,
            4 => state.High,
            5 => state.Low,
            6 => bus.Read(state.HL),
            7 => state.Accumulator,
            _ => throw new NotImplementedException(),
        };
    }

    private static bool IsLoad16Operation(byte opCode)
    {
        var highNibble = opCode >> 4;
        var lowNibble = opCode & 0x0f;

        return opCode switch
        {
            0x08 or 0xf8 or 0xf9 => true,
            _ when highNibble < 4 && lowNibble is 1 => true,
            _ => false
        };
    }

    private byte Load16(byte opCode)
    {
        if (opCode is 0x08)
        {
            var startAddress = ReadImmediateAddress();
            var sp = state.StackPointer;
            bus.Write(startAddress, sp.GetLowByte());
            bus.Write((ushort)(startAddress + 1), sp.GetHighByte());
            return 20;
        }

        if (opCode is 0xf8)
        {
            var result = (ushort)(state.StackPointer + ReadImmediateByte());
            state.High = result.GetHighByte();
            state.Low = result.GetLowByte();
            return 12;
        }

        if (opCode is 0xf9)
        {
            state.StackPointer = state.HL;
            return 8;
        }

        var value = ReadImmediateAddress();
        var high = value.GetHighByte();
        var low = value.GetLowByte();
        switch (opCode & 0xf0)
        {
            case 0:
                state.B = high;
                state.C = low;
                break;
            case 1:
                state.D = high;
                state.E = high;
                break;
            case 2:
                state.High = high;
                state.Low = low;
                break;
            case 3:
                state.StackPointer = value;
                break;
        }
        return 12;
    }

    private static bool IsPopOperation(byte opCode)
    {
        // 0x[c-f]1
        const byte mask = 0b1100_1111;
        const byte result = 0b1100_0001;
        return (opCode & mask) is result;
    }

    private byte Pop(byte opCode)
    {
        var low = bus.Read(state.StackPointer++);
        var high = bus.Read(state.StackPointer++);

        switch (opCode & 0xf0)
        {
            case 0xc0:
                state.B = high;
                state.C = low;
                break;
            case 0xd0:
                state.D = high;
                state.E = low;
                break;
            case 0xe0:
                state.High = high;
                state.Low = low;
                break;
            case 0xf0:
                state.Accumulator = high;
                state.Flags = low; // & 0xF0 ??
                break;
        }

        return 12;
    }

    private static bool IsPushOperation(byte opCode)
    {
        // 0x[c-f]5
        const byte mask = 0b1100_1111;
        const byte result = 0b1100_0101;
        return (opCode & mask) is result;
    }

    private byte Push(byte opCode)
    {
        byte low = 0;
        byte high = 0;

        switch (opCode & 0xf0)
        {
            case 0xc0:
                high = state.B;
                low = state.C;
                break;
            case 0xd0:
                high = state.D;
                low = state.E;
                break;
            case 0xe0:
                high = state.High;
                low = state.Low;
                break;
            case 0xf0:
                high = state.Accumulator;
                low = state.Flags;
                break;
        }

        bus.Write(--state.StackPointer, low);
        bus.Write(--state.StackPointer, high);
        return 16;
    }

    private static bool IsIncrement8Operation(byte opCode)
    {
        // 0x[0-3](4|c) = 0b00xx_x100
        const byte mask = 0b1100_0111;
        const byte result = 0b0000_0100;
        return (opCode & mask) is result;
    }

    private byte Increment8(byte opCode)
    {
        byte oldValue;
        byte duration = 4;

        if (opCode is 0x34)
        {
            oldValue = bus.Read(state.HL);
            bus.Write(state.HL, (byte)(oldValue + 1));
            duration = 12;
        }
        else
        {
            oldValue = opCode switch
            {
                0x04 => state.B++,
                0x0c => state.C++,

                0x14 => state.D++,
                0x1c => state.E++,

                0x24 => state.High++,
                0x2c => state.Low++,

                0x3c => state.Accumulator++,
                _ => 0
            };
        }

        state.Flags = new CpuFlagRegister(state.Flags)
                .SetTo(CpuFlags.HalfCarry, (oldValue & 0xf) is 0xf)
                .SetTo(CpuFlags.Zero, oldValue is 0xff)
                .Unset(CpuFlags.Subtract);

        return duration;
    }

    private static bool IsIncrement16Operation(byte opCode)
    {
        // 0x[0-3]3 = 0b00xx_0011
        const byte mask = 0b1100_1111;
        const byte result = 0b0000_0011;
        return (opCode & mask) is result;
    }

    private byte Increment16(byte opCode)
    {
        switch (opCode & 0xf0)
        {
            case 0x00:
                if (state.C++ is 0xff)
                {
                    state.B++;
                }
                break;
            case 0x10:
                if (state.E++ is 0xff)
                {
                    state.D++;
                }
                break;
            case 0x20:
                if (state.Low++ is 0xff)
                {
                    state.High++;
                }
                break;
            case 0x30:
                state.StackPointer++;
                break;
        }
        return 8;
    }

    private static bool IsDecrement8Operation(byte opCode)
    {
        // 0x[0-3](5|d) = 0b00xx_x101
        const byte mask = 0b1100_0111;
        const byte result = 0b0000_0101;
        return (opCode & mask) is result;
    }

    private byte Decrement8(byte opCode)
    {
        byte oldValue;
        byte duration = 4;

        if (opCode is 0x35)
        {
            oldValue = bus.Read(state.HL);
            bus.Write(state.HL, (byte)(oldValue - 1));
            duration = 12;
        }
        else
        {
            oldValue = opCode switch
            {
                0x05 => state.B--,
                0x0d => state.C--,

                0x15 => state.D--,
                0x1d => state.E--,

                0x25 => state.High--,
                0x2d => state.Low--,

                0x3d => state.Accumulator--,
                _ => 0
            };
        }

        state.Flags = new CpuFlagRegister(state.Flags)
                .SetTo(CpuFlags.HalfCarry, (oldValue & 0xf) is 0)
                .SetTo(CpuFlags.Zero, oldValue is 1)
                .Set(CpuFlags.Subtract);

        return duration;
    }

    private static bool IsDecrement16Operation(byte opCode)
    {
        // 0x[0-3]b = 0b00xx_1011
        const byte mask = 0b1100_1111;
        const byte result = 0b0000_1011;
        return (opCode & mask) is result;
    }

    private byte Decrement16(byte opCode)
    {
        switch (opCode & 0xf0)
        {
            case 0x00:
                if (state.C-- is 0)
                {
                    state.B--;
                }
                break;
            case 0x10:
                if (state.E-- is 0)
                {
                    state.D--;
                }
                break;
            case 0x20:
                if (state.Low-- is 0)
                {
                    state.High--;
                }
                break;
            case 0x30:
                state.StackPointer--;
                break;
        }

        return 8;
    }

    private static bool IsRotateOperation(byte opCode)
    {
        // 0x(0|1)(7|f) = 0b000x_x111
        const byte mask = 0b1110_0111;
        const byte result = 0b0000_0111;
        return (opCode & mask) is result;
    }

    private byte Rotate(byte opCode)
    {
        var isRightShift = (opCode & 0xf) is 0xf;
        var useWrapAround = (opCode & 0xf0) is 0;

        var wrapAroundAddition = (byte)(isRightShift
            ? state.Accumulator << 7
            : state.Accumulator >> 7);

        if (isRightShift)
        {
            state.Accumulator >>= 1;
        }
        else
        {
            state.Accumulator <<= 1;
        }

        if (useWrapAround)
        {
            state.Accumulator |= wrapAroundAddition;
        }

        state.Flags = new CpuFlagRegister(state.Flags)
            .Unset(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Carry, wrapAroundAddition > 0);

        return 4;
    }

    private static bool IsRestartOperation(byte opCode)
    {
        // 0x[c-f](7|f) = 0b11xx_x111
        const byte mask = 0b1100_0111;
        return (opCode & mask) is mask;
    }

    private byte Restart(byte opCode)
    {
        bus.Write(--state.StackPointer, state.ProgramCounter.GetLowByte());
        bus.Write(--state.StackPointer, state.ProgramCounter.GetHighByte());

        state.ProgramCounter = opCode switch
        {
            0xc7 => 0,
            0xcf => 8,
            0xd7 => 0x10,
            0xdf => 0x18,
            0xe7 => 0x20,
            0xef => 0x28,
            0xf7 => 0x30,
            _ => 0x38
        };

        return 16;
    }

    private static bool IsConditionalReturnOperation(byte opCode)
    {
        // 0x(c|d)(0|8) = 0b110x_x000
        const byte mask = 0b1110_0111;
        const byte result = 0b1100_0000;
        return (opCode & mask) is result;
    }

    private byte ConditionalReturn(byte opCode)
    {
        var flags = new CpuFlagRegister(state.Flags);

        if (opCode is 0xc0 && flags.IsSet(CpuFlags.Zero))
        {
            return 8;
        }
        else if (opCode is 0xd0 && flags.IsSet(CpuFlags.Carry))
        {
            return 8;
        }
        else if (opCode is 0xc8 && flags.IsNotSet(CpuFlags.Zero))
        {
            return 8;
        }
        else if (opCode is 0xd8 && flags.IsNotSet(CpuFlags.Carry))
        {
            return 8;
        }

        var low = bus.Read(state.StackPointer++);
        state.ProgramCounter = bus.Read(state.StackPointer++).Concat(low);

        return 20;
    }

    private static bool IsReturnOperation(byte opCode)
    {
        // 0x(c|d)9 = 0b110x_1001
        const byte mask = 0b1110_1111;
        const byte result = 0b1100_1001;
        return (opCode & mask) is result;
    }

    private byte Return(byte opCode)
    {
        if (opCode is 0xd9)
        {
            state.InterruptMasterEnable = true;
        }

        var low = bus.Read(state.StackPointer++);
        state.ProgramCounter = bus.Read(state.StackPointer++).Concat(low);

        return 16;
    }

    private const byte CallOpCode = 0xcd;
    private static bool IsCallOperation(byte opCode)
    {
        // 0x(c|d)(4|c) = 0b110x_x100
        const byte mask = 0b1110_1111;
        const byte result = 0b1100_0100;
        return opCode is CallOpCode
            || (opCode & mask) is result;
    }

    private byte Call(byte opCode)
    {
        bus.Write(state.StackPointer++, state.ProgramCounter.GetLowByte());
        bus.Write(state.StackPointer++, state.ProgramCounter.GetHighByte());

        byte duration = 12;
        var flags = new CpuFlagRegister(state.Flags);

        var nextPC = ReadImmediateAddress();

        if (opCode is CallOpCode
            || (opCode is 0xc4 && flags.IsNotSet(CpuFlags.Zero))
            || (opCode is 0xd4 && flags.IsNotSet(CpuFlags.Carry))
            || (opCode is 0xcc && flags.IsSet(CpuFlags.Zero))
            || (opCode is 0xdc && flags.IsSet(CpuFlags.Carry))
            )
        {
            state.ProgramCounter = nextPC;
            duration += 12;
        }

        return duration;
    }

    private const byte JumpRelativeOpCode = 0x18;
    private static bool IsRelativeJumpOperation(byte opCode)
    {
        // 0x(2|3)(0|8) = 0b001x_x000
        const byte mask = 0b1110_0111;
        const byte result = 0b0010_0000;
        return opCode is JumpRelativeOpCode
            || (opCode & mask) is result;
    }

    private byte RelativeJump(byte opCode)
    {
        byte duration = 8;
        var flags = new CpuFlagRegister(state.Flags);

        var jumpBy = (sbyte)ReadImmediateByte();

        if (opCode is JumpRelativeOpCode
            || (opCode is 0x20 && flags.IsNotSet(CpuFlags.Zero))
            || (opCode is 0x30 && flags.IsNotSet(CpuFlags.Carry))
            || (opCode is 0x28 && flags.IsSet(CpuFlags.Zero))
            || (opCode is 0x38 && flags.IsSet(CpuFlags.Carry))
            )
        {
            state.ProgramCounter = (ushort)(state.ProgramCounter + jumpBy);
            duration += 4;
        }

        return duration;
    }

    private const byte JumpDirectOpCode = 0xc3;
    private const byte JumpOpCode = 0xe9;
    private static bool IsExactJumpOperation(byte opCode)
    {
        // 0x(c|d)(2|a) = 0b110x_x010
        const byte mask = 0b1110_0111;
        const byte result = 0b1100_0010;
        return opCode is JumpDirectOpCode or JumpOpCode
            || (opCode & mask) is result;
    }

    private byte JumpTo(byte opCode)
    {
        byte duration = 12;
        var flags = new CpuFlagRegister(state.Flags);

        var nextPC = ReadImmediateAddress();

        if (opCode is JumpDirectOpCode
            || (opCode is 0xc2 && flags.IsNotSet(CpuFlags.Zero))
            || (opCode is 0xd2 && flags.IsNotSet(CpuFlags.Carry))
            || (opCode is 0xca && flags.IsSet(CpuFlags.Zero))
            || (opCode is 0xda && flags.IsSet(CpuFlags.Carry))
            )
        {
            state.ProgramCounter = nextPC;
            duration += 4;
        }

        return duration;
    }

    private static bool IsLogicalOperation(byte opCode)
    {
        // 0x[c-f](6|e) = 0b11xx_x110
        const byte mask = 0b1100_0111;
        const byte result = 0b1100_0110;
        return opCode is > 0x7f and < 0xc0
            || (opCode & mask) is result;
    }

    private byte ExecuteLogic(byte opCode)
    {
        var flags = new CpuFlagRegister(state.Flags);
        byte duration = 4;

        var value = GetSourceValue(opCode, out var didReadFromMemory);
        if (didReadFromMemory)
        {
            duration += 4;
        }

        if (opCode < 0xa0)
        {
            var isWithCarry = (opCode & 8) is 8 && flags.IsSet(CpuFlags.Carry);

            if (opCode < 0x90)
            {
                var result = state.Accumulator + value;
                var halfResult = (state.Accumulator & 0xf) + (value & 0xf);

                if (isWithCarry)
                {
                    result++;
                    halfResult++;
                }

                state.Accumulator = (byte)result;

                state.Flags = flags.SetTo(CpuFlags.Carry, result > 0xff)
                    .SetTo(CpuFlags.HalfCarry, halfResult > 0xf)
                    .SetTo(CpuFlags.Zero, state.Accumulator is 0)
                    .Unset(CpuFlags.Subtract);
            }
            else
            {
                var result = state.Accumulator - value;
                var halfResult = (state.Accumulator & 0xf) - (value & 0xf);

                if (isWithCarry)
                {
                    result--;
                    halfResult--;
                }

                state.Accumulator = (byte)result;

                state.Flags = flags.SetTo(CpuFlags.Carry, result < 0)
                    .SetTo(CpuFlags.HalfCarry, halfResult < 0)
                    .SetTo(CpuFlags.Zero, state.Accumulator is 0)
                    .Set(CpuFlags.Subtract);
            }
        }
        else if (opCode < 0xa8)
        {
            state.Accumulator &= value;

            state.Flags = flags.Set(CpuFlags.HalfCarry)
                .Unset(CpuFlags.Subtract | CpuFlags.Carry)
                .SetTo(CpuFlags.Zero, state.Accumulator is 0);
        }
        else if (opCode < 0xb0)
        {
            state.Accumulator ^= value;

            state.Flags = flags.Unset(CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry)
                .SetTo(CpuFlags.Zero, state.Accumulator is 0);
        }
        else if (opCode < 0xb8)
        {
            state.Accumulator |= value;

            state.Flags = flags.Unset(CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry)
                .SetTo(CpuFlags.Zero, state.Accumulator is 0);
        }
        else if (opCode < 0xc0)
        {
            var result = state.Accumulator - value;
            var halfResult = (state.Accumulator & 0xf) - (value & 0xf);

            state.Flags = flags.Set(CpuFlags.Subtract)
                .SetTo(CpuFlags.Zero, result is 0)
                .SetTo(CpuFlags.Carry, result < 0)
                .SetTo(CpuFlags.HalfCarry, halfResult < 0);
        }

        return duration;
    }

    private static bool IsAdd16Operation(byte opCode)
    {
        // 0x[0-3]9 = 0b00xx_1001
        const byte mask = 0b1100_1111;
        const byte result = 0b0000_1001;
        return (opCode & mask) is result;
    }

    private byte Add16(byte opCode)
    {
        var addition = opCode switch
        {
            0x09 => state.BC,
            0x19 => state.DE,
            0x29 => state.HL,
            0x39 => state.StackPointer,
            _ => throw new Exception()
        };

        var result = state.HL + addition;
        var halfResult = (addition & 0x0fff) + (state.HL & 0x0fff);

        var flags = new CpuFlagRegister(state.Flags);
        flags.SetTo(CpuFlags.Carry, result > 0xffff)
            .SetTo(CpuFlags.HalfCarry, halfResult > 0x0fff)
            .Unset(CpuFlags.Subtract);

        state.Low = (byte)(result & 0xff);
        state.High = (byte)((result >> 8) & 0xff);

        return 8;
    }

    private byte ExecuteCbInstruction(byte opCode)
    {
        state.IsInCbMode = false;

        ref var destination = ref GetDestinationByteRef(opCode, out var destinationIsInMemory);

        var flags = new CpuFlagRegister(state.Flags);

        if (opCode < 9)
        {
            // RotateLeftCarry
            var carry = destination >> 7;
            destination <<= 1;
            destination |= (byte)carry;

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x10)
        {
            // RotateRightCarry
            var carry = destination << 7;
            destination >>= 1;
            destination |= (byte)carry;

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x18)
        {
            // Rotate left
            var carry = destination >> 7;
            destination <<= 1;
            if (flags.IsSet(CpuFlags.Carry))
            {
                destination |= 1;
            }

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x20)
        {
            // Rotate right
            var carry = destination << 7;
            destination >>= 1;
            if (flags.IsSet(CpuFlags.Carry))
            {
                destination |= 0x80;
            }

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x28)
        {
            // Shift left aritmetic
            var carry = destination >> 7;
            destination <<= 1;

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x30)
        {
            // Shift right aritmetic (b7 = b7)
            var carry = destination << 7;
            destination = (byte)((destination >> 1) | (destination & 0x80));

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x38)
        {
            // Swap nibbles
            destination = destination.SwapNibbles();

            state.Flags = flags.Unset(CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry)
                .SetTo(CpuFlags.Zero, destination is 0);
        }
        else if (opCode < 0x40)
        {
            // Shift right Logical (b7 = 0)
            var carry = destination << 7;
            destination >>= 1;

            state.Flags = flags
            .Unset(CpuFlags.Subtract | CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, destination is 0)
            .SetTo(CpuFlags.Carry, (byte)carry > 0);
        }
        else if (opCode < 0x80)
        {
            // Test bit
            var bitNumber = (opCode - 0x40) / 8;
            var result = destination.IsBitSet(bitNumber);

            state.Flags = flags
            .Unset(CpuFlags.Subtract)
            .Set(CpuFlags.HalfCarry)
            .SetTo(CpuFlags.Zero, !result);
        }
        else if (opCode < 0xc0)
        {
            // Reset bit
            var bitNumber = (opCode - 0x80) / 8;
            destination = destination.UnsetBit(bitNumber);
        }
        else
        {
            // Set bit
            var bitNumber = (opCode - 0xc0) / 8;
            destination = destination.SetBit(bitNumber);
        }

        return (byte)(destinationIsInMemory ? 16 : 8);
    }

    private ref byte GetDestinationByteRef(byte opCode, out bool destinationIsInMemory)
    {
        destinationIsInMemory = false;

        var first3bits = opCode & 7;

        if (first3bits is 0)
        {
            return ref state.B;
        }
        if (first3bits is 1)
        {
            return ref state.C;
        }
        if (first3bits is 2)
        {
            return ref state.D;
        }
        if (first3bits is 3)
        {
            return ref state.E;
        }
        if (first3bits is 4)
        {
            return ref state.High;
        }
        if (first3bits is 5)
        {
            return ref state.Low;
        }
        if (first3bits is 6)
        {
            destinationIsInMemory = true;
            return ref bus.GetRef(state.HL);
        }
        if (first3bits is 7)
        {
            return ref state.Accumulator;
        }
        throw new Exception();
    }

    private const byte NopCode = 0x00;
    private const byte NopDuration = 4;

    private const byte StopCode = 0x10;
    private const byte StopDuration = 4;

    private const byte HaltCode = 0x76;
    private const byte HaltDuration = 4;

    private const byte PrefixCbCode = 0xcb;
    private const byte PrefixCbDuration = 4;

    private const byte DisableInterruptCode = 0xf3;
    private const byte DisableInterruptDuration = 4;

    private const byte EnableInterruptCode = 0xfb;
    private const byte EnableInterruptDuration = 4;

    private const byte DaaInstruction = 0x27;
    private const byte DaaDuration = 4;

    private const byte SetCarryFlagInstruction = 0x37;
    private const byte SetCarryFlagDuration = 4;

    private const byte ComplementInstruction = 0x2f;
    private const byte ComplementDuration = 4;

    private const byte ComplementCarryFlagInstruction = 0x3f;
    private const byte ComplementCarryFlagDuration = 4;

    private const byte AddToStackPointerInstruction = 0xe8;
    private const byte AddToStackPointerDuration = 16;
}