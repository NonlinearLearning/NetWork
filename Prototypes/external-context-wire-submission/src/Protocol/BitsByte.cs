namespace Terraria.NetWork.Core.Protocol;

// 这个结构用于模拟旧协议中的 BitsByte。
// 旧协议会把多个布尔值压缩进一个字节，所以这里提供一个最小实现。
public struct BitsByte
{
    // 这里保存实际的位值。
    private byte _value;

    // 这里允许直接用一组布尔值构造一个位字段。
    public BitsByte(params bool[] bits)
    {
        // 先把内部字节清零。
        _value = 0;
        // 这里按顺序把传入的布尔值写进 0 到 7 位。
        for (var i = 0; i < bits.Length && i < 8; i++)
        {
            // 通过索引器统一设置每一位。
            this[i] = bits[i];
        }
    }

    // 这里通过索引器按位读取或写入。
    public bool this[int index]
    {
        // 读取时通过按位与判断目标位是否为 1。
        readonly get => (_value & (1 << index)) != 0;
        set
        {
            // 如果目标值为 true，就把对应位置 1。
            if (value)
            {
                _value = (byte)(_value | (1 << index));
            }
            // 如果目标值为 false，就把对应位清 0。
            else
            {
                _value = (byte)(_value & ~(1 << index));
            }
        }
    }

    // 这里允许 BitsByte 隐式转成 byte，便于直接写入字节流。
    public static implicit operator byte(BitsByte value) => value._value;

    // 这里允许 byte 隐式转成 BitsByte，便于从字节流直接读取。
    public static implicit operator BitsByte(byte value) => new() { _value = value };

    // 这里把位字段格式化成十六进制字符串，方便调试输出。
    public override readonly string ToString() => _value.ToString("X2");
}
