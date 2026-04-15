#if Newtonsoft
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace HData.Primitives {
    public static class JTokenUtil {
        public static bool IsNull(this JToken token) {
            return token == null || token.Type == JTokenType.Null;
        }

        public static bool IsNotNull(this JToken token) {
            return token != null && token.Type != JTokenType.Null;
        }

        public static T GetValueWithDefault<T>(this JToken token, T value = default) {
            return token.IsNotNull() ? token.Value<T>() : value;
        }

        public static IEnumerable<T> GetValuesWithDefault<T>(this JToken token, T[] values = null, bool create = true) {
            return token.IsNotNull() ? token.Values<T>() : values ?? (create ? new T[0] : null);
        }

        public static Vector2 ToVector2(this JToken token, float defaultValue = -1) {
            if (token.IsNull()) return new Vector2(-1, -1);
            float x = token["x"].GetValueWithDefault(defaultValue);
            float y = token["y"].GetValueWithDefault(defaultValue);
            return new Vector2(x, y);
        }

        public static Vector2Int ToVector2Int(this JToken token, int defaultValue = -1) {
            if (token.IsNull()) return new Vector2Int(-1, -1);
            int x = token["x"].GetValueWithDefault(defaultValue);
            int y = token["y"].GetValueWithDefault(defaultValue);
            return new Vector2Int(x, y);
        }
    }
}
#endif