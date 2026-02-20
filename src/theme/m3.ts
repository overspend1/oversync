import { 
  argbFromHex, 
  themeFromSourceColor, 
  applyTheme 
} from "@material/material-color-utilities";

export const createM3Theme = (sourceColorHex: string, isDark: boolean) => {
  const theme = themeFromSourceColor(argbFromHex(sourceColorHex));
  
  // Apply theme to document
  applyTheme(theme, { target: document.documentElement, dark: isDark });
  
  // Export tokens for Tailwind/Inline styles if needed
  return theme;
};

export const defaultSeedColor = "#6750A4"; // M3 Baseline purple
