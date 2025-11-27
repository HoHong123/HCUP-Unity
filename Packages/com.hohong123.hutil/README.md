# HUtil 0.2.0 릴리즈 노트

## 개요
공용 유틸리티 패키지로, 풀링·데이터·수학·로거·폰트·웹·씬 전환·Tween 보조 등을 포함합니다. Unity 2021.3+ 환경을 대상으로 하며 외부 의존성으로 UniTask, DoTween, Odin Inspector, TextMeshPro를 사용합니다.

## 0.2.0에서 변경된 점
- 중복된 스크립트를 제거해 런타임 구성요소를 26개로 슬림화했습니다.
- 패키지 버전을 0.2.0으로 올려 HGame/HUI와 정렬했습니다.
- 폴더 맵·의존성·주의 사항을 README에 추가했습니다.

## 폴더 맵 (Runtime)
- Collection(3): 순환 리스트 등 보조 자료구조.
- Core(2), Data(1), Debug(1), Font(1), Logger(1), Mathx(1): 공통 상수·도우미 모듈.
- Pooling(9): UnityPool 기반 객체/컴포넌트 풀.
- Primitives(2), Scene(1), Web(3): 수학/씬 전환/웹 요청 유틸.
- Tween: DoTween 연동 보조.
- Editor: Odin/Inspector 툴 1개 (런타임 분리 주의).

## 업그레이드/사용 가이드
- 풀에서 가져온 객체는 반드시 Return 처리합니다.
- 풀·사운드 등 싱글톤 초기화 순서를 씬 로드 시점에 맞춰 관리합니다.
- Editor 스크립트가 빌드에 포함되지 않도록 asmdef·폴더 분리를 유지합니다.
- 외부 의존성(UniTask/DoTween/Odin/TMP)이 누락되지 않았는지 확인합니다.
