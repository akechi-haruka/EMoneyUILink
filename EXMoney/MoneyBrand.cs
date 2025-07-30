namespace Haruka.Arcade.EXMoney;

public class MoneyBrand {
    public uint ID { get; }
    public string Name { get; }
    public string SoundOnScan { get; }
    public string SoundOnSuccess { get; }
    public string SoundOnFailure { get; }
    public bool HasBalance { get; }
    public bool IsPaseli { get; }

    public MoneyBrand(uint id, string name, string soundOnScan, string soundOnSuccess, string soundOnFailure, bool hasBalance, bool isPaseli) {
        ID = id;
        Name = name;
        SoundOnScan = soundOnScan;
        SoundOnSuccess = soundOnSuccess;
        SoundOnFailure = soundOnFailure;
        HasBalance = hasBalance;
        IsPaseli = isPaseli;
    }
}