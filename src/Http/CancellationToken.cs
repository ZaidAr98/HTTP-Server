

namespace src.Http
{
    public class CancellationToken
    {
        private bool _isCancellationRequested = false;

        public bool IsCancellationRequested => _isCancellationRequested;

        public void Cancel()
        {
            _isCancellationRequested = true;
        }
    }
}