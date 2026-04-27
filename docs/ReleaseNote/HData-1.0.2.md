# HData 1.0.2 릴리즈 노트

----

어셈블리 링크 : https://github.com/HoHong123/HCUP-Unity.git?path=/HData#HData-1.0.2

## 개요
인코딩/암호화/수학/Primitives 보조 어셈블리입니다.
Base64 인코딩, AES 암호화, Vector 유틸, Enum/Float/String/JToken 헬퍼를 제공합니다.
독립 어셈블리로 외부 의존성이 없습니다.

## 1.0.2에서 변경된 점

----

- 1.0.1 에서 HUtil 로부터 어셈블리 분리.
- 어셈블리 식별자 prefix 를 HCUP.* 로 통일했습니다 (이전: Hohong123.*).

## 폴더 맵 (Runtime)

----

- Encode: Base64TextEncoding, ITextEncoding (텍스트 인코딩 추상화)
- Encrypt: AesEncryptor, IEncryptor (대칭키 암호화 추상화)
- Mathx: VectorUtil
- Primitives: EnumUtil, FloatUtil, JTokenUtil, StringUtil

## 주의사항

----

- HData 는 별도 UPM 패키지가 아닌 sibling 어셈블리입니다.
- AES 암호화 사용 시 키/IV 는 안전한 저장소에 보관하십시오 (이 어셈블리는 키 관리 책임을 갖지 않습니다).
- HUtil 의 AssetHandler / Data 모듈과는 다른 추상화 계층입니다 — 이름은 비슷하지만 인코딩/암호화/Primitives 도메인이 분리되어 있습니다.

Full Changelog: https://github.com/HoHong123/HCUP-Unity/compare/v1.0.0...HData-1.0.2
