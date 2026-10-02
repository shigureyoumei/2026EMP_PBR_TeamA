using UnityEngine;

namespace PBR
{
    public sealed class ReefEnvironmentDetail : MonoBehaviour
    {
        public ExperimentConfig config;
        public GameObject mediumDetails, richDetails;
        void Awake()
        {
            // Decorative complexity is independent of physical hiding geometry and target count.
            if (mediumDetails) mediumDetails.SetActive(config.backgroundDetail >= 1);
            if (richDetails) richDetails.SetActive(config.backgroundDetail >= 2);
        }
    }
}
