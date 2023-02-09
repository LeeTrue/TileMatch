using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tile : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler
{
    public int xPos { get; private set; }
    public int yPos { get; private set; }
    
    private TileMatchManager tileManager;

    [Header("[Tile Item Sprite]")]
    public Image imageSprite;
    private ItemSpriteData tileSpriteData;
    public ItemSpriteData tileItemSprite
    {
        get { return tileSpriteData; }
        set
        {
            tileSpriteData = value;

            if (itemType == ItemType.Row)
            {
                imageSprite.sprite = tileSpriteData.sprite[1];
                animator.Play(createAnimation.name);
            }
            else if (itemType == ItemType.Column)
            {
                imageSprite.sprite = tileSpriteData.sprite[2];
                animator.Play(createAnimation.name);
            }
            else
            {
                imageSprite.sprite = tileSpriteData.sprite[0];
                if (itemType == ItemType.Bomb || itemType == ItemType.Anything)
                    animator.Play(createAnimation.name);
            }
        }
    }

    public Image obstacleSprite;
    public ItemSpriteData tileObstacleSpriteData;

    private ObstacleType _obstacleType = ObstacleType.Null;
    public ObstacleType obstacleType
    {
        get { return _obstacleType; }
        set
        {
            _obstacleType = value;

            obstacleSprite.sprite = tileObstacleSpriteData.sprite[(int)_obstacleType];

            if (_obstacleType == ObstacleType.Null)
            {
                tileObstacleSpriteData = null;
            }
        }
    }

    public bool isSetSprite
    {
        get
        {
            if (tileItemSprite != null) return true;
            else return false;
        }
    }

    public ItemType itemType { get; set; }

    [Header("[Tile Item Animation]")]
    public Animator animator;
    public AnimationClip createAnimation;
    public AnimationClip removeAnimation;

    public bool isRemovedTile { get; private set; }

    private IEnumerator moveCoroutine;

    public void Init(int _x, int _y, TileMatchManager _tileManager, ItemType _itemType)
    {
        xPos = _x;
        yPos = _y;
        tileManager = _tileManager;
        itemType = _itemType;

        // 타일의 아이템이 있는 경우, 배경도 enable
        if ((int)itemType >= (int)ItemType.Standard)
        {
            this.GetComponent<Image>().enabled = true;
        }
    }

    #region Control Tile
    public void OnPointerUp(PointerEventData eventData)
    {
        if (tileManager.ClickedTile != tileManager.ToBeChangedTile)
        {
            tileManager.CheckPossibleChangeTile();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        tileManager.ClickedTile = this;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 전체 체크가 끝날때까지는 toBeChangedTile이 변경되지 않도록 함.
        if (!tileManager.isCheckedTile)
        {
            tileManager.ToBeChangedTile = this;
        }
    }
    #endregion

    #region Move Tile
    public void Move(int _xPos, int _yPos, float _moveTime, bool _isThread = false)
    {
        if (itemType == ItemType.Empty || itemType == ItemType.Obstacle) return;

        if (!_isThread)
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            moveCoroutine = MoveCoroutine(_xPos, _yPos, _moveTime);
            StartCoroutine(moveCoroutine);
        }
        else
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            queue.Enqueue(MoveCoroutine(_xPos, _yPos, _moveTime));

            if (isThreadRunning is false)
            {
                StartCoroutine(ThreadQueue());
            }
        }
    }

    private IEnumerator MoveCoroutine(int _xPos, int _yPos, float _moveTime)
    {
        xPos = _xPos;
        yPos = _yPos;

        Vector3 startPos = transform.position;
        Vector3 endPos = tileManager.GetWorldPosition(_xPos, _yPos);

        for (float t = 0; t <= 1 * _moveTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(startPos, endPos, t / _moveTime);
            yield return null;
        }

        transform.position = endPos;
    }

    // 타일이 다시 돌아가는 애니메이션을 위한 쓰레드
    public bool isThreadRunning { get; private set; } = false;
    Queue<IEnumerator> queue = new Queue<IEnumerator>();

    private IEnumerator ThreadQueue()
    {
        isThreadRunning = true;

        while (queue.Count > 0)
        {
            yield return StartCoroutine(queue.Dequeue());
        }

        isThreadRunning = false;
    }
    #endregion

    #region Remove Tile
    public void RemoveTile()
    {
        isRemovedTile = true;
        StartCoroutine(RemoveCoroutine());

        if (itemType == ItemType.Row)
        {
            tileManager.RemoveRowTile(yPos);
        }
        else if (itemType == ItemType.Column)
        {
            tileManager.RemoveColumnTile(xPos);
        }
        else if (itemType == ItemType.Bomb)
        {
            tileManager.RemoveAroundTile(xPos, yPos);
        }
        else if(itemType == ItemType.Anything)
        {
            tileManager.RemoveSameSpriteTile(tileItemSprite);
        }
    }

    private IEnumerator RemoveCoroutine()
    {
        if (animator)
        {
            animator.Play(removeAnimation.name);

            yield return new WaitForSeconds(removeAnimation.length);

            Destroy(gameObject);
        }
    }
    #endregion
}