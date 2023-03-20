using System.Collections.Generic;
using System.Linq;

namespace Gameboi.Graphics;

public class OldOamWithNewState
{
    private readonly OldSpriteWithNewState[] sprites = new OldSpriteWithNewState[40];

    public OldOamWithNewState(SystemState state)
    {
        for (byte i = 0; i < 40; i++)
        {
            sprites[i] = new OldSpriteWithNewState(i, state);
        }
    }

    public bool IsColorMode { get; set; }

    public OldSpriteWithNewState[] GetSpritesOnLine(byte ly, bool isDoubleHeight)
    {
        var result = new List<OldSpriteWithNewState>();

        foreach (var sprite in sprites)
        {
            if (!sprite.IsWithinScreenWidth())
            {
                continue;
            }
            if (!sprite.IsWithinScreenHeight())
            {
                continue;
            }

            if (sprite.IsIntersectWithLine(ly, isDoubleHeight))
            {
                result.Add(sprite);
            }
        }
        result.Sort(SortByMemoryLocation);
        if (!IsColorMode)
        {
            AdjustForEqualX(result);
        }
        return result.Take(10).Reverse().ToArray();
    }

    private int SortByMemoryLocation(OldSpriteWithNewState a, OldSpriteWithNewState b)
    {
        return a.Nr - b.Nr;
    }

    private void AdjustForEqualX(List<OldSpriteWithNewState> sprites)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            for (int j = 0; j < sprites.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }
                if (System.Math.Abs(sprites[i].X - sprites[j].X) < 8)
                {
                    if (i < j && sprites[i].X > sprites[j].X)
                    {
                        var temp = sprites[i];
                        sprites[i] = sprites[j];
                        sprites[j] = temp;
                    }
                }
            }
        }
    }
}

