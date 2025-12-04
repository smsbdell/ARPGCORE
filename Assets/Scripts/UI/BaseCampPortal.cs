using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseCampPortal : MonoBehaviour
{
    [SerializeField] private string _combatSceneName = "SampleScene";

    private void OnMouseUpAsButton()
    {
        TriggerPortal();
    }

    public void TriggerPortal()
    {
        SceneFlowController controller = SceneFlowController.Instance;
        if (controller != null)
        {
            controller.StartCombatRun();
            return;
        }

        if (!string.IsNullOrWhiteSpace(_combatSceneName))
        {
            SceneManager.LoadSceneAsync(_combatSceneName);
        }
    }
}
