namespace HGame.Sound {
    // This enum is for editor.
    public enum AudioClips : int {
        #region BGM 690000
        #region ======== BGM 690000
        Conquest = 690001,
        Dungeon,
        NewHopeDrumsOnly,
        #endregion
        #endregion

        #region UI 600000 ~ 609999
        #region ======== Common UI 600000
        RelicUp = 600001,
        Error12,
        Click,
        LevelUp,
        #endregion

        #region ======== Lobby UI 600100
        ItemDelete = 600101,
        #endregion
        #endregion

        #region SFX 610000 ~ 659999
        #region ======== Character SFX 610000
        HumanFemaleAttackLong01 = 610001,
        ChaBerserkerVoice,
        ChaBerserkerVoiceOriginal,
        #endregion

        #region ======== Common SFX 620000

        #endregion

        #region ======== Effect SFX 630000
        Dagger01 = 630001,
        Dagger02,
        Recharge,

        SkillGacha00 = 630101,
        SkillGacha01,

        MonEfxDagger00 = 630401,
        MonEfxDagger01,
        #endregion

        #region ======== Monster SFX 640000
        CreatureAncientTreant01Surprised01 = 640001,
        CreatureCow01Death01,
        CreatureOrc02DeathLong02,
        CreatureOrc02Hurt02,
        CreatureParrot01Death01,
        CreatureParrot01Death02,
        CreatureParrot01Surprised01,
        CreaturePhantasm01Death01,
        CreatureRockGiant01Death01,
        CreatureTreeFolk01Tired01,
        BossDead00,
        #endregion
        #endregion

        #region Voice 660000 ~ 689999
        #region ======== Character Voice 660000

        #endregion

        #region ======== Monster Voice 680000

        #endregion
        #endregion
    }
}