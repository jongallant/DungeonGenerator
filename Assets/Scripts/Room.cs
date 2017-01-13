using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public int ID
    {
        get;
        private set;
    }
    public bool IsMainRoom
    {
        get;
        private set;
    }
    public bool IsVisible
    {
        get;
        private set;
    }
    public bool IsLocked
    {
        get;
        private set;
    }

    Color SecondaryColor = new Color(0.8f, 0.8f, 0.8f);
    Color MainColor = new Color(200f / 255f, 150f / 255f, 65 / 255f);
    Color DisabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    SpriteRenderer Background;
    public Rigidbody2D RigidBody2D;
    public Vector3 Center
    {
        get
        {
            return transform.position;
        }
    }
    public Vector3 TopLeft
    {
        get
        {
            return new Vector3(transform.position.x - transform.localScale.x / 2f, transform.position.y + transform.localScale.y / 2f);
        }
    }
    public Vector3 BottomRight
    {
        get
        {
            return new Vector3(transform.position.x + transform.localScale.x / 2f, transform.position.y - transform.localScale.y / 2f);
        }
    }

    public List<Vector2> Connections = new List<Vector2>();

    void Awake()
    {
        Background = GetComponent<SpriteRenderer>();
        RigidBody2D = GetComponent<Rigidbody2D>();

        IsVisible = true;
    }

    public void Init(int id, Vector2 position, int width, int height)
    {
        ID = id;

        transform.position = position;
        transform.localScale = new Vector2(width, height);

    }

    public void SetMain()
    {
        IsMainRoom = true;
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
    }

    public void Snap()
    {
        int x = Mathf.CeilToInt(transform.position.x);
        int y = Mathf.FloorToInt(transform.position.y);

        transform.position = new Vector2(x, y);
    }

    void FixedUpdate()
    {
        if (!IsLocked)
        {
            Snap();
        }
    }

    void Update()
    {
        if (IsVisible)
        {
            if (IsMainRoom)
                Background.color = MainColor;
            else
                Background.color = SecondaryColor;
        }
        else
        {
            Background.color = DisabledColor;
        }
    }

}
