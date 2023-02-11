using System.Collections.Generic;

namespace Gameboi.Graphics;

public static class ImprovedOam
{
    private const int TotalSpriteCount = 40;

    public static IEnumerable<ImprovedSprite> GetSprites(byte[] oam)
    {
        for (var i = 0; i < TotalSpriteCount; i++)
        {
            yield return new(oam, i);
        }
    }
}
