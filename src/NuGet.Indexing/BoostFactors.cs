using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing
{
    public class BoostFactors
    {
        public static readonly float DefaultBoost = 1.0f;

        private float _defaultBoost;
        private Dictionary<string, float> _boostFactors;

        public float this[string field]
        {
            get
            {
                return GetBoost(field);
            }
            set
            {
                SetBoost(field, value);
            }
        }

        public BoostFactors() : this(new Dictionary<string, float>(), DefaultBoost)
        {
        }

        public BoostFactors(float defaultBoost)
            : this(new Dictionary<string, float>(), defaultBoost)
        {
        }

        public BoostFactors(IDictionary<string, float> factors) : this(factors, DefaultBoost)
        {
        }

        public BoostFactors(IDictionary<string, float> factors, float defaultBoost)
        {
            _defaultBoost = defaultBoost;
            _boostFactors = new Dictionary<string,float>(factors, StringComparer.OrdinalIgnoreCase);
        }

        private float GetBoost(string field)
        {
            float boost;
            if (!_boostFactors.TryGetValue(field, out boost))
            {
                return _defaultBoost;
            }
            return boost;
        }

        private void SetBoost(string field, float value)
        {
            _boostFactors[field] = value;
        }
    }
}
