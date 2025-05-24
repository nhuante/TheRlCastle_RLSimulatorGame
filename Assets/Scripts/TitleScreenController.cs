using UnityEngine;

public class TitleScreenController : MonoBehaviour
{
    public Transform camera;
    public Canvas titlePage;
    public Canvas helpPage;
    public SarsaAgentTester agentTester;

    [Header("Camera Move Settings")]
    public float cameraMoveSpeed = 2f;
    public float stopDistance = 0.01f;

    private Vector3 cameraTargetPosition;
    private bool isMoving = false;

    public string gameMode = "title";

    void Start()
    {
        // Initial position
        cameraTargetPosition = camera.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(camera.position, cameraTargetPosition);

        if (isMoving && distance > stopDistance)
        {
            camera.position = Vector3.MoveTowards(
                camera.position,
                cameraTargetPosition,
                cameraMoveSpeed * Time.deltaTime
            );
        }
        else if (isMoving && distance <= stopDistance)
        {
            camera.position = cameraTargetPosition;
            isMoving = false;
        }
    }

    public void onTrainButton_Click()
    {
        cameraTargetPosition = new Vector3(0.3f, 0f, -106.3f);
        gameMode = "train";
        isMoving = true;
        agentTester.setCharacterPositions("train");
    }

    public void OnPlayButton_Click()
    {
        cameraTargetPosition = new Vector3(0.3f, 0f, -106.3f);
        gameMode = "play";
        isMoving = true;
        agentTester.setCharacterPositions("play");
    }

    public void OnHelpButton_Click()
    {
        cameraTargetPosition = new Vector3(-36f, 0f, -106.3f);
        gameMode = "title";
        helpPage.enabled = true;
        titlePage.enabled = false;
        isMoving = true;
    }

    public void OnBackToTitleButton_Click()
    {
        cameraTargetPosition = new Vector3(-36f, 0f, -106.3f);
        gameMode = "title";
        helpPage.enabled = false;
        titlePage.enabled = true;
        isMoving = true;
        agentTester.backToTitle();
    }
}
