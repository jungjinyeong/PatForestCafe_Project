
using Cysharp.Threading.Tasks;
using UniRx;

public interface IResManagement
{
    public void Dispose();
    public int GetMaxLoad();
    public UniTask Preload();
}