
namespace SLLibrary.JsonModel
{
    public class Billing
    {
		public class W4M
		{
			public class Result<T>
			{
				public T result { get; set; }
                public int? errorCode { get; set; }
                public string errorText { get; set; }
            }

			public class Key
			{
				public string key { get; set; }
			}

        }
    }
}
