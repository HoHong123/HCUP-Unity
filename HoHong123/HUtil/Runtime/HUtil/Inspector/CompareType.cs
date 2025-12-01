namespace HUtil.Inspector {
    public enum HCompareType : byte {
        Equals = 1 << 0,
        NotEquals,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }
}