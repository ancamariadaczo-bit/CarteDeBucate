export const API_BASE_URL = "https://localhost:7080";

export const API_ENDPOINTS = {
    recipes: `${API_BASE_URL}/api/recipes`,
    currentUser: `${API_BASE_URL}/api/authentication/me`,
    exchangeCode: `${API_BASE_URL}/api/authentication/exchange-code`,
    mvcLogin: `${API_BASE_URL}/Account/Login`
};
