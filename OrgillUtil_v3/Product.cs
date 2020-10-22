namespace OrgillUtil_v3 {
	public class Product {
		public string SKU { get; private set; }
		public float OnHand { get; private set; }
		public float OrderQty { get; private set; }

		public float WarehouseQty = -1;

		public float Ratio {
			get { return OrderQty / ((OnHand == 0) ? 1.0f : (float)OnHand); }
		}

		public Product(string sku, float onHand, float orderQty) {
			SKU = sku;
			OnHand = onHand;
			OrderQty = orderQty;
		}

		public override string ToString() {
			return $"{SKU}\t{OnHand}\t\t{OrderQty}\t\t{WarehouseQty}";
		}
	}
}
