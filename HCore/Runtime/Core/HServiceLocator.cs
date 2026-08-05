#if UNITY_EDITOR
/* =========================================================
 * 타입 기반 서비스 로케이터입니다.
 * SingletonBehaviour 의 정적 Instance 접근을 대체/보완하며,
 * 서비스 등록·해제·조회를 단일 지점에서 관리합니다.
 *
 * 주의사항 ::
 * 1. 동일 타입의 중복 등록은 거부됩니다. 교체가 필요하면 Unregister 후 Register 하십시오.
 * 2. Domain Reload 비활성(Enter Play Mode Options) 환경을 위해
 *    SubsystemRegistration 시점에 레지스트리를 초기화합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using HDiagnosis.Logger;

namespace HCore {
    public static class HServiceLocator {
        static readonly Dictionary<Type, object> services = new();


        public static int Count => services.Count;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void _ResetOnDomainReload() {
            services.Clear();
        }


        public static bool Register<TService>(TService service) where TService : class {
            if (service == null) {
                HLogger.Error($"[HServiceLocator] Cannot register null service. Type({typeof(TService).Name})");
                return false;
            }
            if (services.ContainsKey(typeof(TService))) {
                HLogger.Error($"[HServiceLocator] Service already registered. Type({typeof(TService).Name})");
                return false;
            }

            services.Add(typeof(TService), service);
            return true;
        }

        public static bool Unregister<TService>() where TService : class {
            return services.Remove(typeof(TService));
        }

        // 등록된 인스턴스와 동일할 때만 해제한다. 다른 인스턴스를 덮어쓰는 사고 방지.
        public static bool Unregister<TService>(TService service) where TService : class {
            if (services.TryGetValue(typeof(TService), out object registered) == false) return false;
            if (ReferenceEquals(registered, service) == false) return false;
            return services.Remove(typeof(TService));
        }

        public static TService Get<TService>() where TService : class {
            if (services.TryGetValue(typeof(TService), out object service)) {
                return (TService)service;
            }

            HLogger.Error($"[HServiceLocator] Service not found. Type({typeof(TService).Name})");
            return null;
        }

        public static bool TryGet<TService>(out TService service) where TService : class {
            if (services.TryGetValue(typeof(TService), out object registered)) {
                service = (TService)registered;
                return true;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<TService>() where TService : class {
            return services.ContainsKey(typeof(TService));
        }

        public static void Clear() {
            services.Clear();
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.08.04
 *
 * 주요 기능 ::
 * 1. 서비스 등록/해제
 *    + Register / Unregister(인스턴스 일치 검사 오버로드 포함)
 * 2. 서비스 조회
 *    + Get / TryGet / IsRegistered
 * 3. Domain Reload 대응
 *    + SubsystemRegistration 시점 정적 레지스트리 리셋
 *
 * 사용법 ::
 * 1. 제공측: HServiceLocator.Register<IMyService>(this);
 * 2. 사용측: HServiceLocator.Get<IMyService>() 또는 TryGet.
 * 3. 제공측 파괴 시: HServiceLocator.Unregister<IMyService>(this);
 * =========================================================
 */
#endif
