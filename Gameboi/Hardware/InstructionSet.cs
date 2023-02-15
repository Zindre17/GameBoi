using Gameboi.Memory.Specials;

namespace Gameboi.Hardware;

public class InstructionSet
{
    private readonly SystemState state;
    private readonly ImprovedBus bus;

    public InstructionSet(SystemState state, ImprovedBus bus)
    {
        this.state = state;
        this.bus = bus;
    }

    private byte ReadNextInstructionByte()
    {
        return bus.Read(state.ProgramCounter++);
    }

    public byte ExecuteInstruction(byte opCode)
    {
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
        // TODO: emulate stop.
        // TODO: emulate stop bug.
        ReadNextInstructionByte();
        return StopDuration;
    }

    private byte Halt()
    {
        // TODO: emulate halt.
        return HaltDuration;
    }

    private byte PrefixCb()
    {
        // TODO: emulate prefix cb.
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

        var acc = state.Accumulator;

        if (currentFlags.IsSet(CpuFlags.Subtract))
        {
            if (setCarry)
                acc -= 0x60;
            if (currentFlags.IsSet(CpuFlags.HalfCarry))
                acc -= 0x6;
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

        state.Accumulator = acc;

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
            < 0x3f and < 0x80 => true,
            0xe0 or 0xe2 or 0xea => true,
            0xf0 or 0xf2 or 0xfa => true,
            _ => false
        };
    }

    private byte Load8(byte opCode)
    {
        return 0;
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
        return 0;
    }

    private static bool IsPopOperation(byte opCode)
    {
        // 0x[c-f]1
        const byte mask = 0b1100_1111;
        const byte result = 0b1100_0001;
        return (opCode & mask) is result;
    }

    private static byte Pop(byte opCode)
    {
        return 0;
    }

    private static bool IsPushOperation(byte opCode)
    {
        // 0x[c-f]5
        const byte mask = 0b1100_1111;
        const byte result = 0b1100_1010;
        return (opCode & mask) is result;
    }

    private static byte Push(byte opCode)
    {
        return 0;
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
        return 0;
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
        return 0;
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
        return 0;
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
        return 0;
    }

    private static bool IsRotateOperation(byte opCode)
    {
        // 0x(0|1)(7|f) = 0b000x_x111
        const byte mask = 0b1110_0111;
        const byte result = 0b0000_0111;
        return (opCode & mask) is result;
    }

    private static byte Rotate(byte opCode)
    {
        return 0;
    }

    private static bool IsRestartOperation(byte opCode)
    {
        // 0x[c-f](7|f) = 0b11xx_x111
        const byte mask = 0b1100_0111;
        return (opCode & mask) is mask;
    }

    private byte Restart(byte opCode)
    {
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        // TODO
        return 0;
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
        return 0;
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
