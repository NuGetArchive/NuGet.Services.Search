using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.Indexing
{
    /// <summary>
    /// Stores boost factors for fields. Basically a dictionary where there is a default value when one isn't otherwise specified.
    /// </summary>
    /// <remarks>
    /// Boost factors increase the importance of a field in the search index. 1.0 indicates the normal relevance level, values
    /// above and below that serve as multipliers. 1.5 == 150% relevant, 0.5 == 50% relevant, etc.
    /// </remarks>
    public class BoostFactors
    {
        /// <summary>
        /// The default boost used if no default boost is specified... by default ;)
        /// </summary>
        public static readonly float DefaultDefaultBoost = 1.0f;

        private Dictionary<string, float> _boostFactors;

        /// <summary>
        /// Gets or sets the boost for the provided field. If no boost has been registered, returns the default boost value
        /// </summary>
        /// <param name="field">The field to get/set the boost for</param>
        /// <returns>The boost for the field</returns>
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

        public float DefaultBoost { get; private set; }

        public BoostFactors()
            : this(new Dictionary<string, float>(), DefaultDefaultBoost)
        {
        }

        public BoostFactors(float defaultBoost)
            : this(new Dictionary<string, float>(), defaultBoost)
        {
        }

        public BoostFactors(IDictionary<string, float> factors) : this(factors, DefaultDefaultBoost)
        {
        }

        public BoostFactors(IDictionary<string, float> factors, float defaultBoost)
        {
            DefaultBoost = defaultBoost;
            _boostFactors = new Dictionary<string,float>(factors, StringComparer.OrdinalIgnoreCase);
        }

        private float GetBoost(string field)
        {
            float boost;
            if (!_boostFactors.TryGetValue(field, out boost))
            {
                return DefaultBoost;
            }
            return boost;
        }

        private void SetBoost(string field, float value)
        {
            _boostFactors[field] = value;
        }
    }
}
