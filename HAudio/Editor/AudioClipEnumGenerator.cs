#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using HAudio.Core;

/* =========================================================
 * @Jason - PKH
 * 카탈로그에서 게임 쪽 AudioClips enum 과 재생 확장 메서드를 생성하는 스크립트입니다.
 *
 * 주요 기능 ::
 * Generate(catalogs, outputDirectory, ns, enumTypeName) - enum + 확장 메서드 2파일 방출.
 * enum 원소 값과 이름 모두 token 을 파싱해 얻는다. Entry.Uid 필드는 읽지 않는다.
 *
 * 사용법 ::
 * SoundToolsWindow 의 "Enum Generator" 탭(AudioClipEnumPanel)이 호출합니다.
 * 출력 위치는 반드시 게임(프로젝트) 어셈블리 폴더여야 합니다 - HCUP 안에 두면 안 됩니다.
 *
 * 주의 ::
 * 1. 생성물은 프로젝트마다 내용이 완전히 다릅니다. HCUP(공유 서브모듈)에 두면 프로젝트끼리
 *    서로의 enum 을 덮어씁니다. 그래서 출력 경로를 인자로 받고 기본값을 두지 않습니다.
 * 2. 이름 충돌(서로 다른 uid 인데 이름부가 같음)은 생성 실패로 처리합니다 - 자동 개명하지
 *    않습니다. 자동 개명은 어느 이름이 어느 클립인지 모르게 만들어 오히려 실수를 늘립니다.
 * 3. 확장 메서드는 enum 과 같은 생성기에서 나오므로 시그니처가 어긋날 수 없습니다.
 * =========================================================
 */

namespace HAudio.Editor {
    public static class AudioClipEnumGenerator {
        #region Public - Types
        public sealed class Result {
            public bool Success;
            public string EnumFilePath;
            public string ExtensionFilePath;
            public int MemberCount;
            public readonly List<string> Errors = new();
        }
        #endregion

        #region Private - Types
        readonly struct Member {
            public readonly int Uid;
            public readonly string Name;
            public readonly string Token;

            public Member(int uid, string name, string token) {
                Uid = uid;
                Name = name;
                Token = token;
            }
        }
        #endregion

        #region Public - Generate
        public static Result Generate(
            IReadOnlyList<AudioCatalogSO> catalogs,
            string outputDirectory,
            string namespaceName,
            string enumTypeName = "AudioClips") {

            var result = new Result();

            if (catalogs == null || catalogs.Count < 1) {
                result.Errors.Add("No catalog supplied.");
                return result;
            }
            if (string.IsNullOrWhiteSpace(outputDirectory)) {
                result.Errors.Add("Output directory is empty. Point it at the game assembly folder, not HCUP.");
                return result;
            }
            if (!AssetDatabase.IsValidFolder(outputDirectory)) {
                result.Errors.Add($"Output directory is not a valid folder. path={outputDirectory}");
                return result;
            }
            if (!_IsValidIdentifier(enumTypeName)) {
                result.Errors.Add($"Enum type name is not a valid C# identifier. name={enumTypeName}");
                return result;
            }

            if (!_TryCollectMembers(catalogs, result, out var members)) {
                return result;
            }

            string enumPath = $"{outputDirectory}/{enumTypeName}.g.cs";
            string extensionPath = $"{outputDirectory}/{enumTypeName}PlayExtensions.g.cs";

            File.WriteAllText(enumPath, _BuildEnumSource(members, namespaceName, enumTypeName), _Utf8WithBom());
            File.WriteAllText(extensionPath, _BuildExtensionSource(namespaceName, enumTypeName), _Utf8WithBom());

            AssetDatabase.ImportAsset(enumPath);
            AssetDatabase.ImportAsset(extensionPath);

            result.Success = true;
            result.EnumFilePath = enumPath;
            result.ExtensionFilePath = extensionPath;
            result.MemberCount = members.Count;
            return result;
        }
        #endregion

        #region Private - Collect
        // 이름 충돌은 실패로 끝낸다 (설계 결정 C). 자동 접두/접미를 붙이면 카탈로그를 고칠
        // 동기가 사라지고, 생성물 이름이 저작자의 의도와 어긋난 채 굳는다.
        private static bool _TryCollectMembers(
            IReadOnlyList<AudioCatalogSO> catalogs,
            Result result,
            out List<Member> members) {

            members = new List<Member>();
            var uidSeen = new Dictionary<int, string>();
            var nameSeen = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var catalog in catalogs) {
                if (!catalog) continue;

                foreach (var entry in catalog.Entries) {
                    if (entry == null) continue;

                    string token = entry.Token;
                    if (string.IsNullOrWhiteSpace(token)) {
                        result.Errors.Add($"[{catalog.name}] Entry has an empty token.");
                        continue;
                    }

                    if (!AudioCatalogSO.TryParseUid(token, out int uid) ||
                        !AudioCatalogSO.TryParseName(token, out string rawName)) {
                        result.Errors.Add($"[{catalog.name}] Token does not follow \"{{uid}}_{{name}}\". token={token}");
                        continue;
                    }

                    string name = _ToIdentifier(rawName);
                    if (!_IsValidIdentifier(name)) {
                        result.Errors.Add($"[{catalog.name}] Token name cannot become a C# identifier. token={token}");
                        continue;
                    }

                    if (uidSeen.TryGetValue(uid, out string uidOwner)) {
                        // 같은 uid 가 같은 token 으로 두 카탈로그에 있는 건 정상(공유 엔트리)이다.
                        if (!string.Equals(uidOwner, token, StringComparison.Ordinal)) {
                            result.Errors.Add($"Uid collision. uid={uid}, tokens=\"{uidOwner}\" vs \"{token}\"");
                        }
                        continue;
                    }

                    if (nameSeen.TryGetValue(name, out string nameOwner)) {
                        result.Errors.Add(
                            $"Name collision. member=\"{name}\", tokens=\"{nameOwner}\" vs \"{token}\". " +
                            "Rename one of the source files - generation is aborted by design.");
                        continue;
                    }

                    uidSeen.Add(uid, token);
                    nameSeen.Add(name, token);
                    members.Add(new Member(uid, name, token));
                }
            }

            if (result.Errors.Count > 0) return false;
            if (members.Count < 1) {
                result.Errors.Add("No valid entry found.");
                return false;
            }

            members.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            return true;
        }
        #endregion

        #region Private - Emit
        private static string _BuildEnumSource(
            IReadOnlyList<Member> members,
            string namespaceName,
            string enumTypeName) {

            var sb = new StringBuilder(1024 + members.Count * 64);
            _AppendHeader(sb, enumTypeName);

            bool hasNamespace = !string.IsNullOrWhiteSpace(namespaceName);
            string indent = hasNamespace ? "    " : string.Empty;
            if (hasNamespace) sb.AppendLine($"namespace {namespaceName} {{");

            sb.AppendLine($"{indent}/// <summary> 카탈로그 token 의 uid 를 원소 값으로 갖는 재생 식별자. </summary>");
            sb.AppendLine($"{indent}public enum {enumTypeName} {{");
            foreach (var member in members) {
                sb.AppendLine($"{indent}    /// <summary> {member.Token} </summary>");
                sb.AppendLine($"{indent}    {member.Name} = {member.Uid},");
            }
            sb.AppendLine($"{indent}}}");

            if (hasNamespace) sb.AppendLine("}");
            return sb.ToString();
        }

        private static string _BuildExtensionSource(string namespaceName, string enumTypeName) {
            var sb = new StringBuilder(2048);
            _AppendHeader(sb, $"{enumTypeName}PlayExtensions");

            sb.AppendLine("using Cysharp.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using HAudio;");
            sb.AppendLine();

            bool hasNamespace = !string.IsNullOrWhiteSpace(namespaceName);
            string indent = hasNamespace ? "    " : string.Empty;
            if (hasNamespace) sb.AppendLine($"namespace {namespaceName} {{");

            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// {enumTypeName} 를 AudioManager 의 uid 오버로드로 잇는다.");
            sb.AppendLine($"{indent}/// (int) 캐스팅은 IL 명령어를 생성하지 않으므로 런타임 비용이 0 이다.");
            sb.AppendLine($"{indent}/// 확장 메서드로 두는 이유 : 제네릭(where T : Enum)으로 받으면 아무 enum 이나");
            sb.AppendLine($"{indent}/// 통과해 오타 방지라는 도입 목적이 무너진다. 이 방식은 {enumTypeName} 만 받는다.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public static class {enumTypeName}PlayExtensions {{");

            void Method(string signature, string body) {
                sb.AppendLine($"{indent}    public static {signature} => {body};");
            }

            Method($"void Play(this AudioManager manager, {enumTypeName} id)", "manager.Play((int)id)");
            Method($"void PlayUI(this AudioManager manager, {enumTypeName} id)", "manager.PlayUI((int)id)");
            Method($"void Play3D(this AudioManager manager, {enumTypeName} id, Transform parent)", "manager.Play3D((int)id, parent)");
            Method($"void Play3D(this AudioManager manager, {enumTypeName} id, Vector3 worldPos)", "manager.Play3D((int)id, worldPos)");
            Method($"void PlayBGM(this AudioManager manager, {enumTypeName} id, bool ignoreSameClip = true)", "manager.PlayBGM((int)id, ignoreSameClip)");
            Method($"UniTask PrewarmToken(this AudioManager manager, {enumTypeName} id)", "manager.PrewarmToken((int)id)");
            Method($"bool ReleaseToken(this AudioManager manager, {enumTypeName} id)", "manager.ReleaseToken((int)id)");

            // 전역 기본 클릭음도 uid 축이다. 여기에 없으면 게임 코드가 다시 숫자를 쓰게 된다.
            sb.AppendLine($"{indent}    public static void SetGlobalClick({enumTypeName} id) => AudioManager.SetGlobalClickUid((int)id);");

            sb.AppendLine($"{indent}}}");
            if (hasNamespace) sb.AppendLine("}");
            return sb.ToString();
        }

        private static void _AppendHeader(StringBuilder sb, string title) {
            sb.AppendLine("// =============================================================================");
            sb.AppendLine($"//  {title} - 자동 생성 파일입니다. 직접 수정하지 마세요.");
            sb.AppendLine("//  생성기 : HAudio.Editor.AudioClipEnumGenerator");
            sb.AppendLine("//  원본   : AudioCatalogSO 의 token (\"{uid}_{name}\" 규약)");
            sb.AppendLine("//  변경이 필요하면 원본 클립 파일명을 바꾼 뒤 카탈로그와 함께 재생성하세요.");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine();
        }

        private static UTF8Encoding _Utf8WithBom() => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        #endregion

        #region Private - Identifier
        // token 이름부에 C# 식별자로 못 쓰는 문자가 섞이는 경우가 있어 최소 정규화만 한다.
        // 공백·하이픈 등은 '_' 로, 숫자로 시작하면 '_' 를 앞에 붙인다.
        private static string _ToIdentifier(string raw) {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var sb = new StringBuilder(raw.Length + 1);
            foreach (char c in raw.Trim()) {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            string identifier = sb.ToString();
            if (identifier.Length > 0 && char.IsDigit(identifier[0])) identifier = "_" + identifier;
            return identifier;
        }

        private static bool _IsValidIdentifier(string value) {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!char.IsLetter(value[0]) && value[0] != '_') return false;

            for (int k = 1; k < value.Length; k++) {
                if (!char.IsLetterOrDigit(value[k]) && value[k] != '_') return false;
            }

            return true;
        }
        #endregion
    }
}
#endif
