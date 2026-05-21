export type LocalUser = {
  id: string
  email: string
  onboardingDone: boolean
  deletionWarningSentAt: string | null
}

export type InventoryDto = {
  id: string
  userId: string
  name: string
  rowVersion: number
}

export type NutritionFactsDto = {
  id: string
  energyKcal100g: number | null
  carbohydrates100g: number | null
  sugars100g: number | null
  fat100g: number | null
  saturatedFat100g: number | null
  proteins100g: number | null
  salt100g: number | null
  nutritionDataPer: string | null
}

export type ExpiryDateDto = {
  id: string
  estimatedExpiry: string
  isManualOverride: boolean
  overrideDate: string | null
  categoryDefaultUsedDays: number | null
}

export type InventoryItemStatus = 'Available' | 'Low' | 'Expired'
export type AddedVia = 'Receipt' | 'Barcode' | 'Manual'
export type QuantityUnit = 'l' | 'dl' | 'cl' | 'ml' | 'kg' | 'g' | 'mg'

export type InventoryItemDto = {
  id: string
  inventoryId: string
  productName: string
  quantity: number
  quantityUnit: string | null
  ean: string | null
  storageLocation: string | null
  status: InventoryItemStatus
  addedVia: AddedVia
  addedAt: string
  updatedAt: string
  categoryId: number | null
  nutritionFacts: NutritionFactsDto | null
  expiryDate: ExpiryDateDto | null
  rowVersion: number
}

export type CreateInventoryItemRequest = {
  productName: string
  quantity: number
  quantityUnit: QuantityUnit | null
  ean: string | null
  storageLocation: string | null
  addedVia: AddedVia
  categoryId?: number | null
  manualExpiryDate?: string | null
}

export type UpdateInventoryItemRequest = {
  productName: string
  quantity: number
  quantityUnit: QuantityUnit | null
  storageLocation: string | null
  status: InventoryItemStatus
  rowVersion: number
}

export type ProductDto = {
  productName: string
  categoryTags: string[]
  nutrition: {
    energyKcal100g: number | null
    carbohydrates100g: number | null
    sugars100g: number | null
    fat100g: number | null
    saturatedFat100g: number | null
    proteins100g: number | null
    salt100g: number | null
  } | null
  categoryName: string | null
  defaultShelfLifeDays: number | null
}

export type StoreChain = 'Netto' | 'Fotex' | 'Bilka'
export type StoreConnectionStatus = 'Active' | 'Disconnected' | 'PendingSync'

export type StoreConnectionDto = {
  id: string
  userId: string
  chain: StoreChain | number
  status: StoreConnectionStatus | number
  connectedAt: string
  disconnectedAt: string | null
  lastPolledAt: string | null
  tokenExpiresAt: string | null
}

export type StoreConnectionSyncResultDto = {
  connectionId: string
  chain: StoreChain | number
  status: StoreConnectionStatus | number
  syncedAt: string
  importedReceiptCount: number
  processedInventoryItemCount: number
}

export type PendingReceiptDto = {
  dsgReceiptId: string
  storeName: string | null
  receiptType: string | null
  salesTotalDkk: number
  createdAt: string
}

export type ImportSelectedReceiptsDto = {
  selectedDsgReceiptIds: string[]
  importHorizonDate: string | null
}

export type SyncLogDto = {
  id: string
  syncedAt: string
  status: string
  importedReceiptCount: number
  processedInventoryCount: number
}

export type RecipeIngredientDto = {
  productName: string
  quantity: number
  measuringUnit: string | null
  inventoryItemId: string | null
  inInventory: boolean
}

export type RecipeDto = {
  id: string
  name: string
  description: string
  instructions: string
  portions: number
  ingredients: RecipeIngredientDto[]
  isSaved: boolean
}

export type RecipeSuggestionsDto = {
  suggestions: RecipeDto[]
}

export type RecipeListItemDto = {
  id: string
  name: string
  description: string | null
  portions: number
  ingredientCount: number
  ingredientNames: string[]
  isSaved: boolean
}

export type ShoppingListItemDto = {
  id: string
  name: string
  quantity: number | null
  measuringUnit: string | null
  isChecked: boolean
}

export type ShoppingListDto = {
  id: string
  userId: string
  name: string
  createdAt: string
  items: ShoppingListItemDto[]
}
