using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace Unity.VisualScripting
{
    public class RayConverter : fsDirectConverter<Ray>
    {
        protected override fsResult DoSerialize(Ray model, Dictionary<string, fsData> serialized)
        {
            var result = fsResult.Success;

            result += SerializeMember(serialized, null, "origin", model.origin);
            result += SerializeMember(serialized, null, "direction", model.direction);

            return result;
        }

        protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref Ray model)
        {
            var result = fsResult.Success;

            var origin = model.origin;
            result += DeserializeMember(data, null, "origin", out origin);
            model.origin = origin;

            var direction = model.direction;
            result += DeserializeMember(data, null, "direction", out direction);
            model.direction = direction;

            return result;
        }

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Ray();
        }
    }
}
