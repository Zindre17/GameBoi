using Gameboi.Extensions;
using Gameboi.Memory.Specials;

namespace Gameboi.Hardware;

public class ImprovedCpu
{
    private readonly SystemState state;
    private readonly ImprovedBus bus;
    private readonly InstructionSet instructionSet;

    public ImprovedCpu(SystemState state, ImprovedBus bus, InstructionSet instructionSet)
    {
        this.state = state;
        this.bus = bus;
        this.instructionSet = instructionSet;
    }

    private int ticksLeftOfInstruction = 0;

    public void Tick()
    {
        if (ticksLeftOfInstruction is not 0)
        {
            ticksLeftOfInstruction--;
            return;
        }

        if (HandleInterrupts())
        {
            return;
        };

        ExecuteNextInstruction();
    }

    private bool HandleInterrupts()
    {
        var interruptRequests = new InterruptState(state.InterruptFlags);
        if (interruptRequests.HasNone)
        {
            return false;
        }

        state.IsHalted = false;

        if (state.InterruptMasterEnable is false)
        {
            return false;
        }

        var enabledInterrupts = new InterruptState(state.InterruptEnableRegister);

        if (enabledInterrupts.HasNone)
        {
            return false;
        }

        Interrupt();
        return true;
    }

    private void Interrupt()
    {
        state.InterruptMasterEnable = false;

        bus.Write(--state.StackPointer, state.ProgramCounter.GetHighByte());
        bus.Write(--state.StackPointer, state.ProgramCounter.GetLowByte());

        ticksLeftOfInstruction = 20;

        var interrupts = new InterruptState(state.InterruptEnableRegister & state.InterruptFlags);
        if (interrupts.IsVerticalBlankSet)
        {
            state.ProgramCounter = (ushort)InterruptVector.VerticalBlank;
            state.InterruptFlags = state.InterruptFlags.UnsetBit(0);
        }
        else if (interrupts.IsLcdStatusSet)
        {
            state.ProgramCounter = (ushort)InterruptVector.LcdStatus;
            state.InterruptFlags = state.InterruptFlags.UnsetBit(1);
        }
        else if (interrupts.IsTimerSet)
        {
            state.ProgramCounter = (ushort)InterruptVector.Timer;
            state.InterruptFlags = state.InterruptFlags.UnsetBit(2);
        }
        else if (interrupts.IsSerialPortSet)
        {
            state.ProgramCounter = (ushort)InterruptVector.Serial;
            state.InterruptFlags = state.InterruptFlags.UnsetBit(3);
        }
        else if (interrupts.IsJoypadSet)
        {
            state.ProgramCounter = (ushort)InterruptVector.Joypad;
            state.InterruptFlags = state.InterruptFlags.UnsetBit(4);
        }
    }


    private void ExecuteNextInstruction()
    {
        var opCode = Fetch();
        ticksLeftOfInstruction = instructionSet.ExecuteInstruction(opCode);
    }

    private byte Fetch()
    {
        return bus.Read(state.ProgramCounter++);
    }
}
