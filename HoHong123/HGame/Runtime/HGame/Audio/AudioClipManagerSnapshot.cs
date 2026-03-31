#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 Audio.SoundManager preview에 사용되는 에디터 전용 스냅샷 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 에디터 전용 타입이므로 런타임 로딩 경로에는 관여하지 않습니다.
 * 2. 디버그 표시용 데이터만 다루며 재생 제어는 수행하지 않습니다.
 * =========================================================
 */

namespace HGame.Audio {
    [Serializable]
    public sealed class AudioClipManagerSnapshot {
        [Serializable]
        public sealed class Entry {
            public string Token;
            public bool IsLoaded;
            public AudioClip Clip;
            public string ClipName;
            public float ClipLength;
            public List<string> CatalogNames = new List<string>();
        }

        [Serializable]
        public sealed class CatalogGroup {
            public string Name;
            public int RefCount;
            public int EntryCount;
            public int LoadedCount;
        }

        public string ManagerName;
        public int TokenCount;
        public int LoadedCount;
        public int EntryCount;

        public List<CatalogGroup> Catalogs = new List<CatalogGroup>();
        public List<Entry> Entries = new List<Entry>();
    }
}

/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. token, clip, catalog 상태를 하나의 스냅샷으로 정리합니다.
 * 2. 인스펙터 확인에 필요한 중첩 DTO를 제공합니다.
 *
 * 사용법 ::
 * 1. SoundManager preview에서 생성 결과를 표시할 때 사용합니다.
 * 2. 실제 로딩 처리 대신 상태 확인 용도로만 참조합니다.
 *
 * 이벤트 ::
 * 1. 별도의 런타임 이벤트는 없습니다.
 * 2. preview 추적 상태가 바뀌면 다음 스냅샷 결과가 달라집니다.
 *
 * 기타 ::
 * 1. Serializable 구조만 제공합니다.
 * 2. 에디터 가시성 목적의 데이터 컨테이너입니다.
 * =========================================================
 */
#endif
