using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Hardware;

namespace GB_Emulator.Sound.channels
{
    public abstract class SoundChannel : Hardware, IUpdatable
    {
        protected NR52 nr52;
        protected int channelNr;
        protected ModeRegister mode;
        protected int elapsedDurationInCycles;
        protected int remainingDurationInCycles;

        public SoundChannel(NR52 nr52, int channelNr)
        {
            this.nr52 = nr52;
            this.channelNr = channelNr;
        }

        public override void Connect(Bus bus)
        {
            this.bus = bus;
            bus.RegisterUpdatable(this);
        }

        public virtual void Update(byte cycles, ulong speed)
        {
            var elapsed = (int)(cycles / speed);
            elapsedDurationInCycles += elapsed;

            if (remainingDurationInCycles <= 0)
            {
                nr52.TurnOff(channelNr);
            }

            if (mode.IsInitial)
            {
                mode.IsInitial = false;
                nr52.TurnOn(channelNr);
                elapsedDurationInCycles = 0;
                remainingDurationInCycles = GetDurationInCycles();
                OnInit();
            }
            if (mode.HasDuration)
            {
                remainingDurationInCycles -= elapsed;
            }
        }

        protected abstract int GetDurationInCycles();
        protected abstract void OnInit();
    }
}