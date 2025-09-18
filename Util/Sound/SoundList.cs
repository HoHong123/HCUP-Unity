// Use real file name.
public enum SFXList {
    #region ----- Common -----
    Click = 100,
    Notification,
    CashRegister,
    #endregion

    #region ----- Effect -----
    Frozen = 10000,
    Poison,
    Thunder,
    #endregion

    #region ----- Player -----
    PlayerAtk_100100 = 100100,
    PlayerAtk_100101,
    PlayerAtk_100102,
    PlayerAtk_100103,

    PlayerHit_101100 = 101100,

    PlayerSummon_102100 = 102100,

    PlayerDead_102100 = 103100,
    #endregion

    #region ----- Monster Attack -----
    MonsterAtk = 500100,
    #endregion

    #region ----- Monster Dead -----
    MonsterDead_500004 = 501100,
    MonsterDead_500010_500018,
    MonsterDead_500020_500028,
    MonsterDead_500030_500038,
    #endregion

    #region ----- Mercenary_Attack -----
    MercAtk1 = 1000100,
    MercAtk2,
    MercAtk3,
    #endregion

    #region ----- Mercenary_Place -----
    MercPlace_111010_111012 = 1001100,
    MercPlace_111020_111022,
    MercPlace_111025_111027,
    MercPlace_111030_111032,
    MercPlace_111100_111104,
    MercPlace_112010_112012,
    MercPlace_112020_112022,
    MercPlace_112025_112027,
    MercPlace_112100_112104,
    MercPlace_113010_113014,
    MercPlace_113020_113022,
    MercPlace_113025_113027,
    MercPlace_113030_113032,
    #endregion
}

public enum BGMList {
    Stage1 = 11000100,
}

public enum SoundType {
    SFX,
    BGM,
}
