using UnityEngine;

public class SC_InventorySystem : MonoBehaviour
{
    [Header("References")]
    public Texture crosshairTexture;
    public SC_CharacterController playerController;
    public SC_PickItem[] availableItems; // List of all available item prefabs

    [Header("Inventory Settings")]
    private int[] itemSlots = new int[12];
    private bool showInventory = false;
    private float windowAnimation = 1f;
    private float animationTimer = 0f;

    // UI Drag & Drop
    private int hoveringOverIndex = -1;
    private int itemIndexToDrag = -1;
    private Vector2 dragOffset = Vector2.zero;

    // Item Pickup
    private SC_PickItem detectedItem;
    private int detectedItemIndex = -1;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize item slots
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i] = -1;
        }
    }

    void Update()
    {
        // Toggle inventory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showInventory = !showInventory;
            animationTimer = 0f;

            Cursor.visible = showInventory;
            Cursor.lockState = showInventory ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // Inventory window animation
        if (animationTimer < 1f)
            animationTimer += Time.deltaTime;

        if (showInventory)
        {
            windowAnimation = Mathf.Lerp(windowAnimation, 0f, animationTimer);
            playerController.canMove = false;
        }
        else
        {
            windowAnimation = Mathf.Lerp(windowAnimation, 1f, animationTimer);
            playerController.canMove = true;
        }

        HandleItemDragAndDrop();
        HandleItemPickup();
    }

    private void HandleItemDragAndDrop()
    {
        // Begin dragging
        if (Input.GetMouseButtonDown(0) && hoveringOverIndex > -1 && itemSlots[hoveringOverIndex] > -1)
        {
            itemIndexToDrag = hoveringOverIndex;
        }

        // Release dragged item
        if (Input.GetMouseButtonUp(0) && itemIndexToDrag > -1)
        {
            if (hoveringOverIndex < 0)
            {
                // Drop item into the world
                Instantiate(
                    availableItems[itemSlots[itemIndexToDrag]],
                    playerController.playerCamera.transform.position + playerController.playerCamera.transform.forward,
                    Quaternion.identity
                );
                itemSlots[itemIndexToDrag] = -1;
            }
            else
            {
                // Swap items
                int temp = itemSlots[itemIndexToDrag];
                itemSlots[itemIndexToDrag] = itemSlots[hoveringOverIndex];
                itemSlots[hoveringOverIndex] = temp;
            }

            itemIndexToDrag = -1;
        }
    }

    private void HandleItemPickup()
    {
        if (detectedItem && detectedItemIndex > -1 && Input.GetKeyDown(KeyCode.F))
        {
            int slotToAddTo = -1;
            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (itemSlots[i] == -1)
                {
                    slotToAddTo = i;
                    break;
                }
            }

            if (slotToAddTo > -1)
            {
                itemSlots[slotToAddTo] = detectedItemIndex;
                detectedItem.PickItem();
            }
        }
    }

    void FixedUpdate()
    {
        // Detect if the player is looking at an item
        Ray ray = playerController.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 2.5f))
        {
            Transform objectHit = hit.transform;

            if (objectHit.CompareTag("Respawn") && objectHit.TryGetComponent(out SC_PickItem itemTmp))
            {
                if (detectedItem == null || detectedItem.transform != objectHit)
                {
                    for (int i = 0; i < availableItems.Length; i++)
                    {
                        if (availableItems[i].itemName == itemTmp.itemName)
                        {
                            detectedItem = itemTmp; // fixed casing error
                            detectedItemIndex = i;
                            return;
                        }
                    }
                }
            }
            else
            {
                detectedItem = null;
                detectedItemIndex = -1;
            }
        }
        else
        {
            detectedItem = null;
            detectedItemIndex = -1;
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(5, 5, 200, 25), "Press 'Tab' to open Inventory");

        // Inventory window animation
        if (windowAnimation < 1f)
        {
            GUILayout.BeginArea(new Rect(10 - (430 * windowAnimation), Screen.height / 2 - 200, 302, 430), GUI.skin.box);
            GUILayout.Label("Inventory", GUILayout.Height(25));

            GUILayout.BeginVertical();
            for (int i = 0; i < itemSlots.Length; i += 3)
            {
                GUILayout.BeginHorizontal();

                for (int a = 0; a < 3; a++)
                {
                    if (i + a >= itemSlots.Length) break;

                    if (itemIndexToDrag == i + a || (itemIndexToDrag > -1 && hoveringOverIndex == i + a))
                        GUI.enabled = false;

                    if (itemSlots[i + a] > -1)
                    {
                        Texture preview = availableItems[itemSlots[i + a]].itemPreview;
                        GUILayout.Box(preview ? preview : (Texture)Texture2D.whiteTexture, GUILayout.Width(95), GUILayout.Height(95));
                    }
                    else
                    {
                        GUILayout.Box("", GUILayout.Width(95), GUILayout.Height(95));
                    }

                    // Hover detection
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    Vector2 eventMousePosition = Event.current.mousePosition;
                    if (Event.current.type == EventType.Repaint && lastRect.Contains(eventMousePosition))
                    {
                        hoveringOverIndex = i + a;
                        if (itemIndexToDrag < 0)
                        {
                            dragOffset = new Vector2(lastRect.x - eventMousePosition.x, lastRect.y - eventMousePosition.y);
                        }
                    }

                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // If cursor is outside inventory area
            if (Event.current.type == EventType.Repaint && !GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                hoveringOverIndex = -1;
            }

            GUILayout.EndArea();
        }

        DrawItemDragPreview();
        DrawHoverItemName();
        DrawCrosshairAndPickupPrompt();
    }

    private void DrawItemDragPreview()
    {
        if (itemIndexToDrag < 0) return;

        var item = availableItems[itemSlots[itemIndexToDrag]];
        Rect boxRect = new Rect(Input.mousePosition.x + dragOffset.x, Screen.height - Input.mousePosition.y + dragOffset.y, 95, 95);

        if (item.itemPreview)
            GUI.Box(boxRect, item.itemPreview);
        else
            GUI.Box(boxRect, item.itemName);
    }

    private void DrawHoverItemName()
    {
        if (hoveringOverIndex > -1 && itemSlots[hoveringOverIndex] > -1 && itemIndexToDrag < 0)
        {
            GUI.Box(
                new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y - 30, 100, 25),
                availableItems[itemSlots[hoveringOverIndex]].itemName
            );
        }
    }

    private void DrawCrosshairAndPickupPrompt()
    {
        if (showInventory) return;

        GUI.color = detectedItem ? Color.green : Color.white;
        GUI.DrawTexture(new Rect(Screen.width / 2 - 4, Screen.height / 2 - 4, 8, 8), crosshairTexture);
        GUI.color = Color.white;

        if (detectedItem)
        {
            string message = $"Press 'F' to pick '{detectedItem.itemName}'";
            GUI.color = new Color(0, 0, 0, 0.84f);
            GUI.Label(new Rect(Screen.width / 2 - 75 + 1, Screen.height / 2 - 50 + 1, 150, 20), message);
            GUI.color = Color.green;
            GUI.Label(new Rect(Screen.width / 2 - 75, Screen.height / 2 - 50, 150, 20), message);
        }
    }
}
