namespace Tests;

[TestClass]
public class TimerTests
{
    private readonly SystemState state = new();

    [TestMethod]
    public void Tick()
    {
        var timer = new Gameboi.Timing.Timer(state);

        state.TimerCounter = 0;
        state.Tima = 0;
        state.Tma = 40;

        // enable timer & use same increment speed as div;
        state.Tac = 7;

        for (var i = 0; i < 255; i++)
        {

            timer.Tick();

            Assert.AreEqual(0, state.Div);
            Assert.AreEqual(0, state.Tima);
        }


        timer.Tick();

        Assert.AreEqual(1, state.Div);
        Assert.AreEqual(1, state.Tima);

        for (var i = 0; i < (256 * 0xff) - 1; i++)
        {

            timer.Tick();
        }
        Assert.AreEqual(0xff, state.Div);
        Assert.AreEqual(0xff, state.Tima);


        timer.Tick();

        Assert.AreEqual(0, state.Div);
        Assert.AreEqual(40, state.Tima);

        state.Tac = 4;

        for (var i = 0; i < 1023; i++)
        {

            timer.Tick();

            Assert.AreEqual(40, state.Tima);
        }

        timer.Tick();

        Assert.AreEqual(41, state.Tima);

        state.Tac = 5;

        for (var i = 0; i < 15; i++)
        {

            timer.Tick();

            Assert.AreEqual(41, state.Tima);
        }

        timer.Tick();

        Assert.AreEqual(42, state.Tima);

        state.Tac = 6;

        for (var i = 0; i < 63; i++)
        {

            timer.Tick();

            Assert.AreEqual(42, state.Tima);
        }

        timer.Tick();

        Assert.AreEqual(43, state.Tima);
    }
}
