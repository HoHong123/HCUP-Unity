namespace HUtil.Data.Sequence {
    enum AddressableLoadType : byte {
        LabelAll = 0,

        LabelFirst = 1,
        LabelSingle,
        LabelIndex,

        Address = 4,
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 1. LabelFirst
 * + 레이블 결과가 단일 or 다수의 경우, 첫 번째 결과 반환
 * 2. LabelSingle
 * + 레이블 결과 단일일 경우에만 결과 반환
 * 3. LabelIndex
 * + 레이블 결과 단일 or 다수일 경우, 결과 중 n번째 인덱스 값 반환
 * 4. Address
 * + 주소 결과 반환
 * =========================================================
 */
#endif
