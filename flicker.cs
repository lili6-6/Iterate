using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

namespace PP
{
    public class Flicker : MonoBehaviour
    {
        [SerializeField] private Light2D light2D;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private float flickerRange = 0.5f; // ��˸��ǿ�ȷ�Χ
        [SerializeField] private float floatRange = 0.5f;
        [SerializeField] private float scaleRange = 0.3f;
        [SerializeField] private float outerRange = 0.3f;
        [SerializeField]private bool positionFlicker = false;
        [SerializeField]private bool scaleFlicker = false;

        private float originalIntensity; // ԭʼ����
        private float originalPositionY; // ԭʼY��λ��
        private float originalScale;
        private Tweener flickerTweener; // ����������
        private Tweener floatTweener;

        void Start()
        {
            // ����Ƿ�ֵ��Light2D���
            if (light2D == null)
            {
                light2D = GetComponent<Light2D>();
                if (light2D == null)
                {
                    Debug.LogError("û�и�Flicker�ű�����Light2D�����");
                    return;
                }
            }

            // �����ʼֵ��ʹ��Inspector�����õ�ʵ��ֵ��
            originalIntensity = light2D.intensity;
            originalPositionY = transform.position.y;
            originalScale = transform.localScale.x;
            //SetUniformRandomScale();

            //Debug.Log($"��ʼ����: {originalIntensity}, ��ʼYλ��: {originalPositionY}");
            SetUniformRandomOuter();
            StartFlickering();
           
            
        }

        private void StartFlickering()
        {
            // ֹͣ���ж���
            flickerTweener?.Kill();
            floatTweener?.Kill();

            // �������ȵ������ޣ�����ԭʼֵ��
            float minIntensity = Mathf.Max(0, originalIntensity - flickerRange);
            float maxIntensity = originalIntensity + flickerRange;

            // ����λ�õ������ޣ�����ԭʼֵ��
            float minPositionY = originalPositionY - floatRange;
            float maxPositionY = originalPositionY + floatRange;

if(!scaleFlicker) return;
            // ������˸�������ӵ�ǰֵ��ʼ���ڷ�Χ�����أ�
            flickerTweener = DOTween.To(
                () => light2D.intensity,
                x => light2D.intensity = x,
                maxIntensity,
                duration
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

if(!positionFlicker) return;
            // λ�ø������������ð汾��
            floatTweener = DOTween.To(
                () => transform.position.y,
                y => transform.position = new Vector3(transform.position.x, y, transform.position.z),
                maxPositionY,
                duration * 5 // λ�ø�����һ�㣬����Ȼ
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

          
        }
        private void SetUniformRandomScale()
        {
            // ������һ���������ֵ������С�����ȱ�����֮��
            float randomUniformValue = Random.Range(originalScale-scaleRange, originalScale+scaleRange);

            // ��XYZ��������ͬ�����ֵ��ʵ�ֵȱ�����
            transform.localScale = new Vector3(randomUniformValue, randomUniformValue, randomUniformValue);

            // ��ѡ������̨��ӡ��ǰ�ȱ�����ֵ��������Բ鿴
            //Debug.Log($"����ʱ������õĵȱ�����ֵ��{randomUniformValue}��XYZ��һ�£�");
        }
        private void SetUniformRandomOuter()
        {
            float randomUniformValue = Random.Range(light2D.pointLightOuterRadius - outerRange, light2D.pointLightOuterRadius + outerRange);
            light2D.pointLightOuterRadius = randomUniformValue;
            
        }

        // ����ʱֹͣ�������ָ���ʼֵ
        public void OnDisable()
        {
            flickerTweener?.Kill();
            floatTweener?.Kill();

            // �ָ���ʼ״̬
            if (light2D != null)
            {
                light2D.intensity = originalIntensity;
            }
            //transform.position = new Vector3(transform.position.x, originalPositionY, transform.position.z);
        }

        // ����ʱ���¿�ʼ
        private void OnEnable()
        {
            if (light2D != null)
            {
                StartFlickering();
            }
        }

        // ����ʱ�ָ���ʼֵ
        private void OnReset()
        {
            light2D = GetComponent<Light2D>();
        }
    }
}