using UnityEngine;

public class SC_PickItem : MonoBehaviour
{
	public string itemName = "Some Item"; //Each item must have a unique nameof
	public Texture itemPreview;
	
    // Start is called before the first frame update
    void Start()
    {
    //Change item tag to Respawn to detect when we look at item
		gameObject.tag = "Respawn";
    }

    public void PickItem()
	{
		Destroy(gameObject);
	}
}