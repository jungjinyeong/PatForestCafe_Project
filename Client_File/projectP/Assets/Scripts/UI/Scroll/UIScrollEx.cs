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

        var layout = content.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layout == null)
        {
            layout = mIsHorizontal
                ? content.gameObject.AddComponent<HorizontalLayoutGroup>()
                : (HorizontalOrVerticalLayoutGroup)content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = mSpacing;
        layout.padding = mPadding;
        layout.childForceExpandWidth = mChildForceExpandWidth;
        layout.childForceExpandHeight = mChildForceExpandHeight;
        // childControl을 켜두면(Unity가 AddComponent로 새로 붙일 때의 기본값) LayoutGroup이 각 행의
        // width/height를 자기 마음대로 재계산해버려 행 프리팹에 authoring된 "셀 크기"(예: 160x160)와
        // 실제 표시 크기가 어긋난다. 꺼서 각 행이 자기 RectTransform 크기를 그대로 유지하게 한다.
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // LayoutGroup만으로는 Content 자신의 RectTransform 크기(width/height)가 자식 합계에 맞춰 자라지 않는다
        // (프리팹에 박제된 sizeDelta에 그대로 머무름 — 그래서 0으로 보임). ContentSizeFitter가 두 축 모두를
        // 자식들의 실제 셀 크기 합/최대값으로 매 레이아웃마다 재계산하게 한다(childControl이 꺼져 있어 자식
        // 크기가 authoring값 그대로이므로, 두 축 다 PreferredSize로 둬도 고정 sizeDelta에 기대지 않고 정확하다).
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

        // LayoutGroup/ContentSizeFitter는 Unity의 다음 레이아웃 리빌드 타이밍에 자동으로 반영되는데,
        // 풀링된 행을 SetActive(true)로 재사용하는 이 흐름에선 그 타이밍이 늦어(같은 프레임에 ScrollRect가
        // 갱신 전 크기를 읽는 경우 등) Content 크기가 갱신되지 않은 채로 보일 수 있다 — SetData() 직후
        // 즉시 강제로 재계산해서 셀 개수만큼 Content 크기가 확실히 반영되게 한다.
        if (mScrollRect.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(mScrollRect.content);
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
