using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Moimo.Modules
{
    public class UIBehaviour : ObjectBehaviour
    {
        [FoldoutGroup("Base")] [BoxGroup("Base/Manual Remove")] [SerializeField]
        private Button removeBtn;
        
        public override void Awake()
        {
            base.Awake();
            removeBtn?.onClick.AddListener(Remove);
        }
    }
}
