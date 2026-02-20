import { 
  argbFromHex, 
  themeFromSourceColor, 
  applyTheme 
} from "@material/material-color-utilities";

export const createM3Theme = async (sourceColorHex: string, isDark: boolean) => {
  let finalSourceColor = sourceColorHex;

  // Platform-specific dynamic color extraction
  try {
    const { platform } = await import("@tauri-apps/plugin-os");
    const currentPlatform = platform();

    if (currentPlatform === "android") {
      // On Android, we try to use Monet colors if available
      // This usually requires a native plugin, but we can mock it here
      console.log("Extracting Android Monet colors...");
    } else if (currentPlatform === "windows") {
      // On Windows, we can use the accent color
      console.log("Extracting Windows accent colors...");
    }
  } catch (e) {
    console.error("Failed to detect platform for dynamic colors", e);
  }

  const theme = themeFromSourceColor(argbFromHex(finalSourceColor));
  
  // Apply theme to document
  applyTheme(theme, { target: document.documentElement, dark: isDark });
  
  // Export tokens for Tailwind/Inline styles if needed
  return theme;
};

export const defaultSeedColor = "#6750A4"; // M3 Baseline purple
