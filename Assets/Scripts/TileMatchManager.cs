using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum ItemType
{
    Default,    // 제일 초기화 상태

    Empty,  // tile이 생성 X
    
    Standard,   // 기본 tile
    
    Row,    // 같은 행 제거 아이템
    Column, // 같은 열 제거 아이템
    Bomb,   // 주변 9개 제거 아이템
    Anything,   // 같은 그림 제거 아이템
    
    Obstacle_Wood1,
    Obstacle_Wood2,
    Obstacle_Wood3
}

public static class Const
{
    public const int MAX_COUNT = 8;
}

public class TileMatchManager : MonoBehaviour
{
    public int maxRow;
    public int maxColumn;
    public float speed;

    [Header("[Set TileMap]")]
    public Vector2[] emptyTileMap;

    [Header("[Tile Data]")]
    public ItemSpriteData[] spriteDataArr;
    private Dictionary<SpriteType, ItemSpriteData> spriteDataDic;

    public GameObject tilePrefab;

    [Header("[Tile GameObject]")]
    private Tile[,] tiles;

    private Tile clickedTile;

    public Tile ClickedTile
    {
        get { return clickedTile; }
        set { clickedTile = value; }
    }

    private Tile toBeChangedTile;

    public Tile ToBeChangedTile
    {
        get { return toBeChangedTile; }
        set { toBeChangedTile = value; }
    }

    public bool isCheckedTile { get; private set; }

    private void Awake()
    {
        // tile의 sprite 정보를 Dictionary에 세팅
        spriteDataDic = new Dictionary<SpriteType, ItemSpriteData>();
        foreach (ItemSpriteData data in spriteDataArr)
        {
            if (!spriteDataDic.ContainsKey(data.type))
            {
                spriteDataDic.Add(data.type, data);
            }
        }

        maxRow = (maxRow > Const.MAX_COUNT) ? Const.MAX_COUNT : maxRow;
        maxColumn = (maxColumn > Const.MAX_COUNT) ? Const.MAX_COUNT : maxColumn;

        // 맨 처음 전체 타일 생성 (ItemType.Default)
        tiles = new Tile[maxColumn, maxRow];

        // 맵의 모양을 지정하기 위해 Empty 좌표에 타일을 미리 생성
        foreach (Vector2 empty in emptyTileMap)
        {
            CreateNewTile((int)empty.x, (int)empty.y, ItemType.Empty);
        }

        for (int x = 0; x < maxColumn; x++)
        {
            for (int y = 0; y < maxRow; y++)
            {
                if (tiles[x, y] == null)
                {
                    CreateNewTile(x, y, ItemType.Default);
                }
            }
        }

        StartCoroutine(CheckMatchAllTile());
    }

    #region Check Match Tile
    // 전체 매칭이 되는 타일이 있는지 확인
    private IEnumerator CheckMatchAllTile()
    {
        bool isMatchedTile = true;

        // 매칭되는 타일이 없을 때까지 반복
        while (isMatchedTile)
        {
            yield return new WaitForSeconds(speed);
            // 비어있는 타일이 없을때까지 반복
            while (CheckDefaultTile())
            {
                yield return new WaitForSeconds(speed);
            }

            isMatchedTile = RemoveAllMatchTile();
        }

        clickedTile = null;
        toBeChangedTile = null;

        if (isCheckedTile) isCheckedTile = false;
    }

    // 비어 있는(ItemType.Default) 타일이 있는지 확인
    private bool CheckDefaultTile()
    {
        bool isDefaultTile = false;

        // 제일 윗 행이 빈 타일일 경우엔 Standard로 새로 생성
        for (int xPos = 0; xPos < maxColumn; xPos++)
        {
            Tile topTile = tiles[xPos, 0];
            
            if (topTile.itemType == ItemType.Default)
            {
                Destroy(topTile.gameObject);

                GameObject newTile = Instantiate(tilePrefab, GetWorldPosition(xPos, -1), Quaternion.identity, this.transform);
                tiles[xPos, 0] = newTile.GetComponent<Tile>();
                tiles[xPos, 0].Init(xPos, -1, this, ItemType.Standard);
                tiles[xPos, 0].Move(xPos, 0, speed);

                SetTileItemSprite(tiles[xPos, 0], true);

                isDefaultTile = true;
            }
        }

        // 제일 아래 행은 아래 타일과 비교할 필요가 없어 확인하지 않음
        for (int xPos = 0; xPos < maxColumn; xPos++)
        {
            for (int yPos = maxRow - 2; yPos >= 0; yPos--)
            {
                Tile checkTile = tiles[xPos, yPos];
                if (checkTile.itemType == ItemType.Default) continue;

                Tile downTile = tiles[xPos, yPos + 1];
                if (downTile.itemType == ItemType.Empty) continue;

                if (downTile.itemType == ItemType.Default)
                {
                    // check : Empty , down : Default
                    if (checkTile.itemType == ItemType.Empty)
                    {
                        for (int y = yPos - 1; y >= 0; y--)
                        {
                            if (tiles[xPos, y].itemType == ItemType.Default) break;

                            if (tiles[xPos, y].itemType == ItemType.Empty)
                            {
                                if (y > 0) continue; // 최상단이 아닌 경우엔 계속 위의 Tile 비교
                                else // 최상단이 Empty인 경우 새로 생성
                                {
                                    Destroy(downTile.gameObject);
                                    GameObject newTile = Instantiate(tilePrefab, GetWorldPosition(xPos, yPos), Quaternion.identity, this.transform);
                                    tiles[xPos, yPos + 1] = newTile.GetComponent<Tile>();
                                    tiles[xPos, yPos + 1].Init(xPos, yPos, this, ItemType.Standard);
                                    tiles[xPos, yPos + 1].Move(xPos, yPos + 1, speed);

                                    SetTileItemSprite(tiles[xPos, yPos + 1], true);

                                    isDefaultTile = true;
                                    break;
                                }
                            }
                            else
                            {
                                // standard 타일이 있는 경우엔 아래로 내림
                                Destroy(downTile.gameObject);
                                tiles[xPos, y].Move(xPos, yPos + 1, speed);
                                tiles[xPos, yPos + 1] = tiles[xPos, y];
                                CreateNewTile(xPos, y, ItemType.Default);

                                isDefaultTile = true;
                                break;
                            }
                        }                    
                    }
                    // check : Standard , down : Default
                    else
                    {
                        // 아래가 비어있는 경우, 타일을 아래로 움직이고 새로 생성
                        Destroy(downTile.gameObject);
                        checkTile.Move(xPos, yPos + 1, speed);
                        tiles[xPos, yPos + 1] = checkTile;
                        CreateNewTile(xPos, yPos, ItemType.Default);

                        isDefaultTile = true;
                    }
                }
            }
        }

        return isDefaultTile;
    }

    // 선택한 두 타일이 변경 가능한지 확인하고 위치 변경
    public void CheckPossibleChangeTile()
    {
        if (clickedTile.itemType == ItemType.Default || toBeChangedTile.itemType == ItemType.Default) return;
        if (clickedTile.itemType == ItemType.Empty || toBeChangedTile.itemType == ItemType.Empty) return;

        isCheckedTile = true;

        // 상,하,좌,우의 타일일 경우에만 위치 변경
        if ((clickedTile.xPos == toBeChangedTile.xPos && Mathf.Abs(clickedTile.yPos - toBeChangedTile.yPos) == 1) ||
        (clickedTile.yPos == toBeChangedTile.yPos && Mathf.Abs(clickedTile.xPos - toBeChangedTile.xPos) == 1))
        {
            tiles[clickedTile.xPos, clickedTile.yPos] = toBeChangedTile;
            tiles[toBeChangedTile.xPos, toBeChangedTile.yPos] = clickedTile;

            // 움직여서 매칭이 되는게 있거나 특수 아이템 타일을 움직였을 경우
            if (CheckMatchTile(clickedTile, toBeChangedTile) != null || CheckMatchTile(toBeChangedTile, clickedTile) != null ||
                CheckSpecialItemTile(clickedTile, toBeChangedTile))
            {
                int clickedPosX = clickedTile.xPos;
                int clickedPosY = clickedTile.yPos;

                clickedTile.Move(toBeChangedTile.xPos, toBeChangedTile.yPos, speed);
                toBeChangedTile.Move(clickedPosX, clickedPosY, speed);

                #region ItemType.Anything
                if (clickedTile.itemType == ItemType.Anything && toBeChangedTile.itemType == ItemType.Anything)
                {
                    // 둘 다 Anything 아이템일 경우 전체 타일 삭제
                    for (int x = 0; x < maxColumn; x++)
                    {
                        for (int y = 0; y < maxRow; y++)
                        {
                            RemoveTile(x, y);
                        }
                    }
                }
                else
                {
                    if (clickedTile.itemType == ItemType.Anything && toBeChangedTile.isSetSprite)
                    {
                        clickedTile.tileItemSprite = toBeChangedTile.tileItemSprite;

                        RemoveTile(clickedTile.xPos, clickedTile.yPos);
                    }
                    if (toBeChangedTile.itemType == ItemType.Anything && clickedTile.isSetSprite)
                    {
                        toBeChangedTile.tileItemSprite = clickedTile.tileItemSprite;

                        RemoveTile(toBeChangedTile.xPos, toBeChangedTile.yPos);
                    }
                }
                #endregion

                if (clickedTile.itemType == ItemType.Bomb || clickedTile.itemType == ItemType.Row || clickedTile.itemType == ItemType.Column)
                {
                    RemoveTile(clickedTile.xPos, clickedTile.yPos);
                }
                else if (toBeChangedTile.itemType == ItemType.Bomb || toBeChangedTile.itemType == ItemType.Row || toBeChangedTile.itemType == ItemType.Column)
                {
                    RemoveTile(toBeChangedTile.xPos, toBeChangedTile.yPos);
                }

                StartCoroutine(CheckMatchAllTile());
            }
            else
            {
                // 움직여서 매칭이 되는게 없을때 원래 위치로 돌아감
                int clickedPosX = clickedTile.xPos;
                int clickedPosY = clickedTile.yPos;

                clickedTile.Move(toBeChangedTile.xPos, toBeChangedTile.yPos, speed, true);
                toBeChangedTile.Move(clickedPosX, clickedPosY, speed, true);

                tiles[clickedTile.xPos, clickedTile.yPos] = clickedTile;
                tiles[toBeChangedTile.xPos, toBeChangedTile.yPos] = toBeChangedTile;

                clickedPosX = clickedTile.xPos;
                clickedPosY = clickedTile.yPos;

                clickedTile.Move(toBeChangedTile.xPos, toBeChangedTile.yPos, speed * 2, true);
                toBeChangedTile.Move(clickedPosX, clickedPosY, speed * 2, true);

                clickedTile = null;
                toBeChangedTile = null;

                if (isCheckedTile) isCheckedTile = false;
            }
        }
        else
        {
            clickedTile = null;
            toBeChangedTile = null;

            if (isCheckedTile) isCheckedTile = false;
        }
    }

    private bool CheckSpecialItemTile(Tile _tile1, Tile _tile2)
    {
        if (_tile1.itemType == ItemType.Row || _tile1.itemType == ItemType.Column 
            || _tile1.itemType == ItemType.Bomb || _tile1.itemType == ItemType.Anything)
        {
            return true;
        }
        else if (_tile2.itemType == ItemType.Row || _tile2.itemType == ItemType.Column 
            || _tile2.itemType == ItemType.Bomb || _tile2.itemType == ItemType.Anything)
        {
            return true;
        }

        return false;
    }

    // 타일을 변경했을 때 매칭이 되는 타일이 있는지 확인
    private List<Tile> CheckMatchTile(Tile _tile, Tile _changedTile = null)
    {
        int xPos = (_changedTile == null) ? _tile.xPos : _changedTile.xPos;
        int yPos = (_changedTile == null) ? _tile.yPos : _changedTile.yPos;

        List<Tile> matchedTiles = new List<Tile>();

        List<Tile> matchedRowTiles = new List<Tile>();
        List<Tile> matchedColumnTiles = new List<Tile>();

        // 같은 행 타일 매칭 확인하고 매칭된 타일의 열 도 동시 확인
        matchedRowTiles = CheckMatchRowTile(_tile, xPos, yPos);
        if (matchedRowTiles != null)
        {
            matchedTiles.AddRange(matchedRowTiles);

            for (int i = 0; i < matchedRowTiles.Count; i++)
            {
                matchedColumnTiles = CheckMatchColumnTile(_tile, matchedRowTiles[i].xPos, yPos);
                if (matchedColumnTiles != null)
                {
                    matchedTiles.AddRange(matchedColumnTiles);
                    matchedColumnTiles.Clear();
                }
            }
            matchedRowTiles.Clear();
        }

        // 같은 열 타일 매칭 확인하고 매칭된 타일의 행 도 동시 확인
        matchedColumnTiles = CheckMatchColumnTile(_tile, xPos, yPos);
        if (matchedColumnTiles != null)
        {
            matchedTiles.AddRange(matchedColumnTiles);

            for (int i = 0; i < matchedColumnTiles.Count; i++)
            {
                matchedRowTiles = CheckMatchRowTile(_tile, xPos, matchedColumnTiles[i].yPos);
                if (matchedRowTiles != null)
                {
                    matchedTiles.AddRange(matchedRowTiles);
                }
            }
        }

        if (matchedTiles.Count >= 3)
        {
            return matchedTiles;
        }

        return null;
    }

    // 같은 행에 있는 타일 매칭 확인
    private List<Tile> CheckMatchRowTile(Tile _tile, int _xPos, int _yPos)
    {
        List<Tile> matchedRowTiles = new List<Tile>();

        matchedRowTiles.Add(_tile);

        for (int dir = -1; dir < 1; dir++)
        {
            for (int col = 1; col < maxColumn; col++)
            {
                // (dir < 0) => tile 기준 왼쪽 비교, (dir = 0) => tile 기준 오른쪽 비교
                int x = (dir < 0) ? (_xPos - col) : (_xPos + col);

                if (x < 0 || x >= maxColumn) break;

                // 옆의 Tile의 sprite가 동일한 경우, List에 추가하고 옆 계속 추가 비교 
                if (tiles[x, _yPos].tileItemSprite == _tile.tileItemSprite)
                {
                    matchedRowTiles.Add(tiles[x, _yPos]);
                }
                else
                {
                    break;
                }
            }
        }

        if (matchedRowTiles.Count >= 3)
        {
            return matchedRowTiles;
        }

        return null;
    }

    // 같은 열에 있는 타일 매칭 확인
    private List<Tile> CheckMatchColumnTile(Tile _tile, int _xPos, int _yPos)
    {
        List<Tile> matchedColumnTiles = new List<Tile>();

        matchedColumnTiles.Add(tiles[_xPos, _yPos]);

        for (int dir = -1; dir < 1; dir++)
        {
            for (int row = 1; row < maxRow; row++)
            {
                // (dir < 0) => tile 기준 아래쪽 비교, (dir = 0) => tile 기준 위쪽 비교
                int y = (dir < 0) ? (_yPos + row) : (_yPos - row);

                if (y < 0 || y >= maxRow) break;

                // 위아래의 Tile의 sprite가 동일한 경우, List에 추가하고 위아래 계속 추가 비교
                if (tiles[_xPos, y].tileItemSprite == _tile.tileItemSprite)
                {
                    matchedColumnTiles.Add(tiles[_xPos, y]);
                }
                else
                {
                    break;
                }
            }
        }

        if (matchedColumnTiles.Count >= 3)
        {
            return matchedColumnTiles;
        }

        return null;
    }
    #endregion

    #region Remove Tile
    // 매칭된 타일 제거
    private bool RemoveAllMatchTile()
    {
        bool isMatchedTile = false;

        for (int y = 0; y < maxRow; y++)
        {
            for (int x = 0; x < maxColumn; x++)
            {
                if (tiles[x, y].itemType == ItemType.Default) continue;

                List<Tile> matchedTiles = CheckMatchTile(tiles[x, y]);

                if (matchedTiles == null) continue;

                #region Special Item Type (Row, Column, Bomb, Anything)
                // 특수 아이템 생성을 위해 매칭된 타일들 중 랜덤으로 선정, Empty 타일로 선정될 경우 다시 랜덤 선정
                Tile specialTile;
                do
                {
                    specialTile = matchedTiles[Random.Range(0, matchedTiles.Count)];
                } while (specialTile.itemType == ItemType.Empty);

                Vector2 specialTilePos = new Vector2(specialTile.xPos, specialTile.yPos);

                ItemType specialItemType = ItemType.Default;

                if (matchedTiles.Count == 4)
                {
                    if (clickedTile == null || toBeChangedTile == null)
                    {
                        specialItemType = (ItemType)Random.Range((int)ItemType.Row, (int)ItemType.Column);
                    }
                    else if (clickedTile.yPos == toBeChangedTile.yPos)
                    {
                        specialItemType = ItemType.Row;
                    }
                    else
                    {
                        specialItemType = ItemType.Column;
                    }
                }
                else if (matchedTiles.Count == 5)
                {
                    specialItemType = ItemType.Anything;
                }
                else if (matchedTiles.Count >= 6)
                {
                    specialItemType = ItemType.Bomb;
                }
                #endregion

                matchedTiles = matchedTiles.Distinct().ToList();

                // 매칭된 타일 제거
                foreach (Tile tile in matchedTiles)
                {
                    if (!RemoveTile(tile.xPos, tile.yPos)) continue;

                    isMatchedTile = true;

                    if (tile != clickedTile && tile != toBeChangedTile) continue;

                    specialTilePos = new Vector2(tile.xPos, tile.yPos);
                }

                if (specialItemType == ItemType.Default) continue;

                // 매칭된 타일들 중 랜덤으로 특별 아이템 타일 생성
                Destroy(tiles[(int)specialTilePos.x, (int)specialTilePos.y].gameObject);
                Tile newSpecialTile = CreateNewTile((int)specialTilePos.x, (int)specialTilePos.y, specialItemType);

                if ((specialItemType == ItemType.Row || specialItemType == ItemType.Column)
                    && !newSpecialTile.isSetSprite && matchedTiles[0].isSetSprite)
                {
                    SetTileItemSprite(newSpecialTile, false, matchedTiles[0].tileItemSprite.type);
                }
                else if (specialItemType == ItemType.Bomb && !newSpecialTile.isSetSprite)
                {
                    SetTileItemSprite(newSpecialTile, false, SpriteType.Bomb);
                }
                else if (specialItemType == ItemType.Anything && !newSpecialTile.isSetSprite)
                {
                    SetTileItemSprite(newSpecialTile, false, SpriteType.Anything);
                }
            }
        }

        return isMatchedTile;
    }

    // 지정한 좌표 타일 제거
    private bool RemoveTile(int _xPos, int _yPos)
    {
        if (tiles[_xPos, _yPos].itemType == ItemType.Empty || tiles[_xPos, _yPos].itemType == ItemType.Default 
            || tiles[_xPos, _yPos].isRemovedTile) return false;

        tiles[_xPos, _yPos].RemoveTile();
        CreateNewTile(_xPos, _yPos, ItemType.Default);

        return true;

    }

    // 행 전체 타일 제거 (ItemType.Row)
    public void RemoveRowTile(int _rowIndex)
    {
        for (int x = 0; x < maxColumn; x++)
        {
            RemoveTile(x, _rowIndex);
        }
    }

    // 열 전체 타일 제거 (ItemType.Column)
    public void RemoveColumnTile(int _columnIndex)
    {
        for (int y = 0; y < maxRow; y++)
        {
            RemoveTile(_columnIndex, y);
        }
    }

    // 폭탄 타일 제거 (ItemType.Bomb)
    public void RemoveAroundTile(int xPos, int yPos)
    {
        for (int x = xPos - 1; x <= xPos + 1; x++)
        {
            for (int y = yPos - 1; y <= yPos + 1; y++)
            {
                if (x < 0 || x >= maxColumn || y < 0 || y >= maxRow) continue;
                RemoveTile(x, y);
            }
        }
    }

    // 같은 그림 타일 제거 (ItemType.Anything)
    public void RemoveSameSpriteTile(ItemSpriteData _spriteData)
    {
        for (int x = 0; x < maxColumn; x++)
        {
            for (int y = 0; y < maxRow; y++)
            {
                if (tiles[x, y].tileItemSprite == _spriteData || (_spriteData.type == SpriteType.Anything))
                {
                    RemoveTile(x, y);
                }
            }
        }
    }
    #endregion

    #region Create Tile
    // 타일 좌표 계산
    public Vector2 GetWorldPosition(int _xPos, int _yPos)
    {
        return new Vector2(-1 * (maxColumn / 2.0f) + _xPos, (maxRow / 2.0f) - _yPos);
    }

    // 새로 타일 생성
    private Tile CreateNewTile(int _xPos, int _yPos, ItemType _type)
    {
        GameObject newTile = Instantiate(tilePrefab, GetWorldPosition(_xPos, _yPos), Quaternion.identity, this.transform);
        tiles[_xPos, _yPos] = newTile.GetComponent<Tile>();
        tiles[_xPos, _yPos].Init(_xPos, _yPos, this, _type);

        return tiles[_xPos, _yPos];
    }
    
    // 새로 생성한 타일 이미지 세팅
    private void SetTileItemSprite(Tile _tile, bool _isRandom, SpriteType _spriteType = SpriteType.Default)
    {
        if (_isRandom)
        {
            // SpriteType.Anything, Bomb, Empty을 제외한 인덱스 index 3 ~ Length
            _tile.tileItemSprite = spriteDataArr[Random.Range(3, spriteDataArr.Length)];
        }
        else
        {
            if (spriteDataDic.ContainsKey(_spriteType))
            {
                _tile.tileItemSprite = spriteDataDic[_spriteType];
            }
        }
    }
    #endregion
}