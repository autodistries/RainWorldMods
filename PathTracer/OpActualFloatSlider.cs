
using UnityEngine;

namespace Menu.Remix.MixedUI;

public class OpActualFloatSlider : OpFloatSlider
{
    public OpActualFloatSlider(Configurable<float> config, Vector2 pos, int length, byte decimalNum = 1, bool vertical = false) : base(config, pos, length, decimalNum, vertical)
    {
    }
}