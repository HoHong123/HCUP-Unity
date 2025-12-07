// 예외적 파일명 != 내부 값 불일치
public enum SoundSFX {
    #region ----- Common -----
    Click = 600000,
    #endregion

    #region ----- Player -----
    PullArrow = 610000,
    FireArrow,
    HitArrow,
    #endregion

    #region ----- Game -----
    BossWarning = 630000,
    #endregion

    #region ----- Effect -----
    Bomb = 640000,
    #endregion

    #region ----- UI -----
    Selection3To1 = 650000,
    RouletteAppear,
    RouletteSpin,
    RouletteStop,
    #endregion
}