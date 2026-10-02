using UnityEngine;

namespace PBR
{
    // Lobby-only art preview. Decorative fish disappear during experimental blocks.
    public sealed class ReefShowcase : MonoBehaviour
    {
        public FishExperiment experiment;
        GameObject school;
        Transform[] fish;
        readonly Vector3[] homes = {new Vector3(-7,5.2f,13),new Vector3(-4,4.6f,18),new Vector3(-1,6.1f,20),new Vector3(2,5.4f,15),new Vector3(5,4.8f,14),new Vector3(8,6,19)};
        void Start()
        {
            if (experiment.config.persistentSchool) return;
            school=new GameObject("Lobby fish preview"); school.transform.SetParent(transform,false);
            fish=new Transform[homes.Length];
            for(int i=0;i<fish.Length;i++)
            {
                var instance=Instantiate(experiment.config.fishPrefabs[i%experiment.config.fishPrefabs.Length],school.transform);
                fish[i]=instance.transform; fish[i].localPosition=homes[i];
                var animation=instance.GetComponentInChildren<Animation>();
                if(animation && animation.clip) { animation.wrapMode=WrapMode.Loop; animation.Play(); }
            }
        }
        void LateUpdate()
        {
            if(!school) return;
            school.SetActive(!experiment.Running);
            if(experiment.Running) return;
            for(int i=0;i<fish.Length;i++)
            {
                float t=Time.time*.35f+i;
                fish[i].localPosition=homes[i]+new Vector3(Mathf.Sin(t)*.6f,Mathf.Sin(t*1.7f)*.16f,0);
                fish[i].localRotation=Quaternion.Euler(0,i%2==0?75:-75,0);
            }
        }
        void OnDestroy() { if(school) Destroy(school); }
    }
}
