---
script_path: Assets/01_Scripts/HCUP-Unity/HGame/Runtime/HGame/2D/Layer/ParallexLayer.cs
script_name: ParallexLayer
latest_log_id: LOG-20250906-1
total_entries: 2
created: 2026-05-04
updated: 2026-05-04
---

# ParallexLayer Dev Log History

`Assets/01_Scripts/HCUP-Unity/HGame/Runtime/HGame/2D/Layer/ParallexLayer.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). 엔트리가 3 개 이하라 .cs 파일은 변경되지 않았으며, **본 history MD 가 ground truth**.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-04).

==================================
@Jason - PKH 06.09.25 [LOG-20250906-1]
TODO :: Shift 리전의 기능들을 따로 모듈화할 예정.
==================================
@Jason - PKH 05.09.25 [LOG-20250905-1]
1. 유틸 패키지의 순환 리스트를 사용하여 패럴랙스 기능을 구현합니다.
2. 제공된 패럴랙스 이미지 리소스의 개수에 따라 짝수 혹은 홀수 배경타일이 존재할 수 있습니다.
+ 짝수의 경우, (중간 - 1)의 이미지가 센터(시작) 이미지로 인식됩니다.
+ 센터의 위치는 해당 패럴랙스가 시작하는 카메라의 중심으로 설정하시고 좌우에 들어갈 이미지들을 배치하시면 됩니다.
++ Ex) 2개의 이미지 = 1번째가 센터
++ Ex) 4개의 이미지 = 2번째가 센터
++ Ex) 5개의 이미지 = 3번째가 센터
==================================
