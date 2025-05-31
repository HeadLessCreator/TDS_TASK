using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 좀비가 데미지를 입을 때, 머리 위에 떠오르는 텍스트 팝업.
/// - 생성 즉시 위로 살짝 떠오르고, 지정된 시간(lifeTime) 뒤에 자동 파괴.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamagePopup : MonoBehaviour
{
    [Tooltip("팝업이 화면에 떠 있는 총 시간 (초)")]
    public float lifeTime = 1f;

    [Tooltip("위로 떠오르는 속도 (유닛/초)")]
    public float floatSpeed = 1f;

    private TextMeshPro textMesh;

    void Awake()
    {
        // TextMeshPro 컴포넌트 참조
        textMesh = GetComponent<TextMeshPro>();

        // lifeTime(예: 1초) 뒤에 자동으로 이 게임오브젝트를 파괴
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 생성 직후 데미지 값을 설정할 때 호출
    /// </summary>
    public void SetText(string damageText)
    {
        if (textMesh != null)
            textMesh.text = damageText;
    }

    void Update()
    {
        // 매 프레임마다 Y축으로 floatSpeed만큼 위로 떠오름
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
    }
}
