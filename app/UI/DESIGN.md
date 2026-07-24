---
name: Heritage Scholarly Minimalist
colors:
  surface: '#fdf8f8'
  surface-dim: '#ddd9d8'
  surface-bright: '#fdf8f8'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f7f3f2'
  surface-container: '#f1edec'
  surface-container-high: '#ebe7e7'
  surface-container-highest: '#e5e2e1'
  on-surface: '#1c1b1b'
  on-surface-variant: '#444748'
  inverse-surface: '#313030'
  inverse-on-surface: '#f4f0ef'
  outline: '#747878'
  outline-variant: '#c4c7c7'
  surface-tint: '#5f5e5e'
  primary: '#111111'
  on-primary: '#ffffff'
  primary-container: '#262626'
  on-primary-container: '#8e8d8c'
  inverse-primary: '#c8c6c5'
  secondary: '#735c00'
  on-secondary: '#ffffff'
  secondary-container: '#fed65b'
  on-secondary-container: '#745c00'
  tertiary: '#00160a'
  on-tertiary: '#ffffff'
  tertiary-container: '#002d19'
  on-tertiary-container: '#00a267'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e4e2e1'
  primary-fixed-dim: '#c8c6c5'
  on-primary-fixed: '#1b1c1c'
  on-primary-fixed-variant: '#474746'
  secondary-fixed: '#ffe088'
  secondary-fixed-dim: '#e9c349'
  on-secondary-fixed: '#241a00'
  on-secondary-fixed-variant: '#574500'
  tertiary-fixed: '#78fbb6'
  tertiary-fixed-dim: '#59de9b'
  on-tertiary-fixed: '#002111'
  on-tertiary-fixed-variant: '#005232'
  background: '#fdf8f8'
  on-background: '#1c1b1b'
  surface-variant: '#e5e2e1'
typography:
  display-lg:
    fontFamily: EB Garamond
    fontSize: 48px
    fontWeight: '500'
    lineHeight: 56px
    letterSpacing: -0.01em
  headline-lg:
    fontFamily: EB Garamond
    fontSize: 32px
    fontWeight: '500'
    lineHeight: 40px
  headline-lg-mobile:
    fontFamily: EB Garamond
    fontSize: 28px
    fontWeight: '500'
    lineHeight: 36px
  headline-md:
    fontFamily: EB Garamond
    fontSize: 24px
    fontWeight: '500'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.05em
rounded:
  sm: 0.5rem
  DEFAULT: 1rem
  md: 1.5rem
  lg: 2rem
  xl: 3rem
  full: 9999px
spacing:
  unit: 8px
  container-margin-mobile: 20px
  container-margin-desktop: 64px
  gutter: 16px
  section-gap: 48px
---

## Brand & Style
The design system is engineered for an academic, cultural heritage context, blending the archival feel of a museum with the efficiency of modern software. The personality is scholarly, quiet, and deeply respectful of the artifacts it displays. 

The aesthetic leans into **Minimalism** with a **Tactile** edge. It utilizes expansive off-white spaces to simulate high-quality paper, paired with precise, pill-shaped UI elements that feel like polished stones or curated museum tokens. The interface prioritizes content clarity over decorative flourish, ensuring that cultural photography and text remain the focal point.

## Colors
The palette is rooted in a historical "Archival White" background to reduce eye strain and provide a warm, non-clinical environment. 

- **Primary (Deep Charcoal):** Used for all core text and primary iconography to ensure maximum legibility.
- **Accent - Gold:** Reserved for highlights, scholarly achievements, or "Featured" status.
- **Accent - Jade:** Used for discovery, growth, and "Explore" actions.
- **Accent - Lacquer Red:** Used for "Passport" stamps, alerts, or vital historical markers.
- **Neutral/Surface:** A slightly darker beige (#F7F3EC) is used for container backgrounds to create subtle layering against the main background.

## Typography
The system employs a high-contrast typographic pairing. **EB Garamond** provides an authoritative, literary feel for titles and headers, evoking the spirit of printed manuscripts. **Inter** is utilized for all functional text, body copy, and UI labels to maintain high readability and a contemporary utility. 

To maintain academic rigor, use "Body-lg" for long-form essays and "Label-sm" in all-caps for metadata such as dates, locations, and catalog numbers.

## Layout & Spacing
The system uses a **Fluid Grid** model with generous margins to enforce the minimalist aesthetic. 

- **Mobile:** 4-column grid with 20px margins.
- **Tablet/Desktop:** 12-column grid with a maximum content width of 1140px.
- **Rhythm:** All spacing must be a multiple of the 8px base unit. Vertical rhythm should be particularly loose around headers to allow the Garamond typeface "room to breathe."
- **Safe Areas:** Ensure the floating bottom navigation bar does not obscure content by adding a 100px bottom padding to all main scroll views.

## Elevation & Depth
Depth is conveyed through **Tonal Layers** and **Ambient Shadows**. This design system avoids heavy drop shadows in favor of subtle, tinted elevation to keep the interface feeling light and "paper-like."

- **Level 0 (Base):** Off-white (#FDFBF7).
- **Level 1 (Cards):** Surface color (#F7F3EC) with a 1px soft border (#E5E1DA) and no shadow.
- **Level 2 (Floating Nav/Popups):** White background with a 15% opacity primary color shadow, 20px blur, and 4px vertical offset.
- **Interactions:** When a user interacts with a card, it should transition to Level 2 elevation to provide tactile feedback.

## Shapes
The shape language is defined by **Extreme Roundedness**. By using pill-shaped (fully rounded) containers for interactive elements, the UI feels soft and approachable, contrasting the sharp, intellectual nature of the serif typography. 

- **Primary Buttons:** Fully rounded (pill).
- **Cards:** Use `rounded-xl` (1.5rem / 24px) to maintain a friendly, curated feel.
- **Input Fields:** Fully rounded (pill).

## Components

### Buttons
- **Primary:** Pill-shaped, Deep Charcoal background, White text.
- **Secondary:** Pill-shaped, Transparent background, 1.5px Deep Charcoal border.
- **Accent:** Used for specific thematic actions (e.g., "Add to Passport" uses a Red pill).

### Floating Navigation Bar
A high-priority component. It must be a pill-shaped container floating 24px from the bottom of the screen. 
- **Tabs:** Home (Classic icon), Explore (Search/Compass), Passport (Stamp/Seal icon).
- **Active State:** The active icon is highlighted in Gold (#D4AF37) with a small 4px dot indicator underneath.

### Search Bar
Minimalist pill-shaped field. No heavy borders; use a subtle background fill (#F7F3EC) and a thin 1px stroke that darkens on focus.

### Cards
Used for artifacts and articles. Cards should have an aspect ratio of 4:5 or 1:1. The image should be top-aligned with text content below in a padded area.

### Accessibility & Motion
- **Accessibility:** All color pairings meet WCAG AA (4.5:1 ratio). Interactive targets (buttons/links) are a minimum of 44x44px.
- **Motion:** Transitions should be "Stately." Use a slow `cubic-bezier(0.2, 0.8, 0.2, 1)` for page transitions. Elements should fade in and slide up slightly (10px) to simulate a physical reveal.