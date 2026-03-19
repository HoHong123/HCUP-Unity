using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using HUI.ScrollView;

public class SpanGridTesterScroller : SpanningGridRecycleView<DemoCellView, DemoCellData> {
    [Title("Data")]
    public int dataSize = 500;

    [Title("Span")]
    [SerializeField]
    int spanXTargetElement = 3;
    [SerializeField]
    int spanYTargetElement = 7;

    List<DemoCellData> data = new();

    private void Start() {
        _Test();
    }

    private void _Test() {
        for (int k = 0; k < dataSize; k++) {
            int spanX = k % spanXTargetElement == 0 ? 2 : 1;
            int spanY = k % spanYTargetElement == 0 ? 2 : 1;
            data.Add(new(
                $"Object\n" +
                $"No.{k + 1}\n" +
                $"Span ({spanX}, {spanY})", spanX, spanY));
        }
        SetData(data);
    }
}
