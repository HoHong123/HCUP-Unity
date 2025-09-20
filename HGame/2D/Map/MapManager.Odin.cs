#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 미니맵을 출력하는 스크립트입니다.
 * 
 * ** 사용법 **
 * 1. 'MapBoundType'으로 미니맵 범위를 결정하는 방법을 선택할 수 있습니다. (자세한 사항은 해당 열거형 파일 확인)
 * 2. 필수 멤버는 인스펙터에서 반드시 넣어야합니다.
 * 
 * ** 멤버 설명 **
 * ---- 필수 멤버 ----
 * + cam : 미니맵에서 사용될 카메라
 * + boundType : 미니맵 범위를 결정할 방법을 선택하는 열거형
 * ++ worldBoundsB2D : Box2D 콜라이더로 미니맵 범위 선택
 * ++ worldBoundSources : 'IWorldBoundSource'를 상속한 스크립트로 미니맵 범위 선택
 * ++ absolutBound : Rect 값으로 미니맵 범위 선택
 * + camArea : 미니맵에 표현될 카메라가 바로보는 범위를 출력할 이미지 UI 오브젝트 RectTransform
 * + mapArea : 미니맵으로 사용될 이미지 UI 오브젝트 RectTransform
 * + markerPrefab : 미니맵에 표현될 오브젝트들을 나타낼 기본 이미지 UI 오브젝트
 * + defaultMarkerSpt : 재사용되는 마커 오브젝트에 커스텀 Sprite 이미지가 설정되지 않는다면 설정될 기본 Sprite 이미지
 * ---- 옵션 멤버 ----
 * + mapPanel : 미니맵 이미지와 상호작용(클릭, 드래그)을 담당하는 패널 스크립트
 * + markerParent : 마커 오브젝트 풀링에 사용되는 부모 객체
 * + autoFitMinimapAspect : 'mapArea' 이미지 비율을 'boundType'에 설정된 값 비율과 맞추는 옵션
 * + fitPadding : 비율 설정 패딩 값
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Util.Core;
using Util.UI.Panel;
using Util.OdinCompat;
#endif

namespace HGame._2D.Map {
    public partial class MapManager : SingletonBehaviour<MapManager> {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Camera")]
        [SerializeField]
        Camera cam;

        [HeaderOrTitle("Bounds")]
        [SerializeField]
        MapBoundType boundType;
        [SerializeField]
        float worldZ = -10f;
        [SerializeField, ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider2D worldBoundsB2D;
        [SerializeField, ShowIf("boundType", MapBoundType.BoundSource)]
        List<MonoBehaviour> worldBoundSources = new();
        [SerializeField, ShowIf("boundType", MapBoundType.Absolute)]
        Rect absolutBound;

        [HeaderOrTitle("UI")]
        [SerializeField]
        RectTransform camArea;
        [SerializeField]
        RectTransform mapArea;
        [SerializeField]
        ProxyPanel mapPanel;

        [HeaderOrTitle("Marker")]
        [SerializeField]
        Image markerPrefab;
        [SerializeField]
        Sprite defaultMarkerSpt;
        [SerializeField, Tooltip("Must be a child of map")]
        Transform markerParent;

        [HeaderOrTitle("Minimap Auto Fit")]
        [SerializeField]
        bool autoFitMinimapAspect = true;
        [SerializeField, ShowIf("autoFitMinimapAspect")]
        Vector2 fitPadding = new Vector2(8, 8);

        [HeaderOrTitle("Options")]
        [SerializeField]
        bool isYAxisUp = true;
        [SerializeField]
        bool allowDragNavigate = true;
#endif
    }
}
