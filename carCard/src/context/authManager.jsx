
let logoutHandler = null;
export const setLogoutHandler = (handler) => {
  logoutHandler = handler;
};
export const doLogout = () => {
  if (logoutHandler) {
    logoutHandler();
  }
};