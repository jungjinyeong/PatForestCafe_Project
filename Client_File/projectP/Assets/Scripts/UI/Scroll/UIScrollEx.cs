using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 범용 스크롤 뷰 컨트롤러.
/// 사용법:
///   1. GameObject에 ScrollRect를 붙이고, UIScrollEx도 붙인다.
///   2. Init(rowPrefab) 으로 행 프리팹을 등록한다.
///   3. SetData(list) 로 데이터를 넘기면 행을 자동으로 생성/재사용한다.
///   4. 행 프리팹에는 UIScrollRow<T> 상속 컴포넌트가 있어야 한다.
/// </summary>
public class UIScrollEx : UIBase
{
    [SerializeField] private ScrollRect mScrollRect;

    [Header("Layout")]
    [SerializeField] private bool mIsHorizontal = false;
    [SerializeField] private float mSpacing = 0f;
    [SerializeField] private RectOffset mPadding = new RectOffset();
    [SerializeField] private bool mChildForceExpandWidth = true;
    [SerializeField] private bool mChildForceExpandHeight = false;

    private GameObject mRowPrefab;
    private Action<UIScrollRow> mOnSelectAction;
    private readonly List<UIScrollRow> mActiveRows = new();
    private readonly Queue<UIScrollRow> mRowPool = new();

    private void Awake()
    {
        if (mScrollRect == null)
            mScrollRect = GetComponent<ScrollRect>();

        SetupLayoutGroup();
    }

    private void SetupLayoutGroup()
    {
        var content = mScrollRect.content;
        if (content == null) return;
        if (content.GetComponent<HorizontalOrVerticalLayoutGroup>() != null) return;

        HorizontalOrVerticalLayoutGroup layout = mIsHorizontal
            ? content.gameObject.AddComponent<HorizontalLayoutGroup>()
            : (HorizontalOrVerticalLayoutGroup)content.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.spacing = mSpacing;
        layout.padding = mPadding;
        layout.childForceExpandWidth = mChildForceExpandWidth;
        layout.childForceExpandHeight = mChildForceExpandHeight;
    }

    // 재호출(팝업 재오픈 등)에도 깨끗한 상태로 시작하도록, 이전에 남아있던 콘텐츠 자식(예전 행 인스턴스,
    // 손으로 배치해둔 행 템플릿 등)을 전부 지운다. 새로 등록하는 rowPrefab 자신은 지우지 않고,
    // 대신 화면에 그대로 남아 보이지 않도록 비활성화한다(Instantiate 템플릿으로만 쓰임).
    public void Init(GameObject rowPrefab)
    {
        mRowPrefab = rowPrefab;

        mActiveRows.Clear();
        mRowPool.Clear();
        ClearContentChildren();

        if (mRowPrefab != null)
            mRowPrefab.SetActive(false);
    }

    private void ClearContentChildren()
    {
        var content = mScrollRect.content;
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (mRowPrefab != null && child.gameObject == mRowPrefab)
                continue;

            Destroy(child.gameObject);
        }
    }

    public void SetOnSelect(Action<UIScrollRow> onSelectAction)
    {
        mOnSelectAction = onSelectAction;
    }

    /// <summary>
    /// 데이터 목록으로 스크롤 행을 채운다. List<T> 등 IList를 그대로 넘겨도 된다.
    /// </summary>
    public void SetData(IList dataList)
    {
        ReturnAllToPool();

        if (dataList == null) return;

        for (int i = 0; i < dataList.Count; i++)
        {
            var row = GetOrCreateRow();
            row.Setup(i, mOnSelectAction);
            row.SetData(dataList[i]);
            mActiveRows.Add(row);
        }
    }

    public void Clear()
    {
        ReturnAllToPool();
    }

    public int ActiveCount => mActiveRows.Count;

    public UIScrollRow GetRow(int index)
    {
        if (index < 0 || index >= mActiveRows.Count) return null;
        return mActiveRows[index];
    }

    public void ScrollToTop()
    {
        if (mScrollRect != null)
            mScrollRect.verticalNormalizedPosition = 1f;
    }

    public void ScrollToBottom()
    {
        if (mScrollRect != null)
            mScrollRect.verticalNormalizedPosition = 0f;
    }

    private UIScrollRow GetOrCreateRow()
    {
        if (mRowPool.Count > 0)
        {
            var pooled = mRowPool.Dequeue();
            pooled.Active();
            return pooled;
        }

        var obj = Instantiate(mRowPrefab, mScrollRect.content);
        obj.SetActive(true);
        var row = obj.GetComponent<UIScrollRow>();
        if (row == null)
            Logger.Error($"[UIScrollEx] '{mRowPrefab.name}' has no UIScrollRow component.");
        return row;
    }

    private void ReturnAllToPool()
    {
        foreach (var row in mActiveRows)
        {
            if (row == null) continue;
            row.Deative();
            mRowPool.Enqueue(row);
        }
        mActiveRows.Clear();
    }
}
