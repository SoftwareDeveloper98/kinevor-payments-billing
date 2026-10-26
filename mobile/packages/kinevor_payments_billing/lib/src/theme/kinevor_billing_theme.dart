import 'package:flutter/material.dart';

/// "Architectural Obsidian" palette and theme used by the billing module.
abstract final class KinevorBillingTheme {
  /// App background ink.
  static const Color ink = Color(0xFF111317);

  /// Raised surface.
  static const Color surface = Color(0xFF16181D);

  /// Card and sheet surface.
  static const Color surfaceRaised = Color(0xFF1E2127);

  /// Hairline border.
  static const Color border = Color(0xFF2A2E36);

  /// Primary text.
  static const Color text = Color(0xFFF4F5F7);

  /// Secondary text.
  static const Color textMuted = Color(0xFF8E949E);

  /// Brand crimson accent.
  static const Color accent = Color(0xFFFF453A);

  /// Accent pressed state.
  static const Color accentPressed = Color(0xFFE0342A);

  /// Text on accent fills.
  static const Color onAccent = Color(0xFFFFFFFF);

  /// Success green.
  static const Color success = Color(0xFF34D399);

  /// Warning amber.
  static const Color warning = Color(0xFFFBBF24);

  /// Error red.
  static const Color error = Color(0xFFFF6B6B);

  /// Info blue.
  static const Color info = Color(0xFF4DAEFF);

  /// Brand gradient used by avatars and hero cards.
  static const LinearGradient brandGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [accent, Color(0xFFFF8A65)],
  );

  /// Status color used by invoice and payment badges.
  static Color statusColor(String status) => switch (status.toLowerCase()) {
    'paid' => success,
    'open' => info,
    'overdue' || 'pending' || 'partiallyrefunded' => warning,
    'failed' => error,
    'refunded' => info,
    'void' => textMuted,
    _ => info,
  };

  /// Status icon paired with [statusColor] so meaning never rides on color.
  static IconData statusIcon(String status) => switch (status.toLowerCase()) {
    'paid' => Icons.check_circle_outline,
    'open' => Icons.receipt_long_outlined,
    'overdue' => Icons.schedule_outlined,
    'void' => Icons.block_outlined,
    'pending' => Icons.hourglass_empty,
    'partiallyrefunded' => Icons.undo_outlined,
    'refunded' => Icons.replay_outlined,
    'failed' => Icons.error_outline,
    _ => Icons.info_outline,
  };

  /// Icon used for a payment method.
  static IconData paymentMethodIcon(String method) => switch (method) {
    'Cash' => Icons.payments_outlined,
    'BankTransfer' => Icons.account_balance_outlined,
    _ => Icons.credit_card_outlined,
  };

  /// Builds the dark billing theme with Sora typography.
  static ThemeData dark({String fontFamily = 'Sora'}) {
    const scheme = ColorScheme(
      brightness: Brightness.dark,
      primary: accent,
      onPrimary: onAccent,
      secondary: accent,
      onSecondary: onAccent,
      error: error,
      onError: onAccent,
      surface: surface,
      onSurface: text,
      onSurfaceVariant: textMuted,
      surfaceContainerHighest: surfaceRaised,
      surfaceContainerHigh: surfaceRaised,
      surfaceContainerLow: surface,
      surfaceContainerLowest: ink,
      surfaceContainer: surfaceRaised,
      outline: border,
      outlineVariant: border,
    );

    final textTheme = const TextTheme(
      headlineMedium: TextStyle(
        fontSize: 24,
        fontWeight: FontWeight.w800,
        height: 1.2,
      ),
      headlineSmall: TextStyle(
        fontSize: 19,
        fontWeight: FontWeight.w700,
        height: 1.25,
      ),
      titleLarge: TextStyle(
        fontSize: 18,
        fontWeight: FontWeight.w700,
        height: 1.3,
      ),
      titleMedium: TextStyle(
        fontSize: 15,
        fontWeight: FontWeight.w600,
        height: 1.3,
      ),
      bodyLarge: TextStyle(fontSize: 15, height: 1.5),
      bodyMedium: TextStyle(fontSize: 14, height: 1.5),
      bodySmall: TextStyle(fontSize: 12, height: 1.45),
      labelLarge: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
      labelSmall: TextStyle(fontSize: 11, fontWeight: FontWeight.w600),
    ).apply(fontFamily: fontFamily, bodyColor: text, displayColor: text);

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      fontFamily: fontFamily,
      colorScheme: scheme,
      scaffoldBackgroundColor: ink,
      canvasColor: ink,
      textTheme: textTheme,
      appBarTheme: AppBarTheme(
        backgroundColor: ink,
        foregroundColor: text,
        elevation: 0,
        centerTitle: false,
        surfaceTintColor: Colors.transparent,
        titleTextStyle: textTheme.headlineSmall?.copyWith(
          fontWeight: FontWeight.w800,
        ),
      ),
      cardTheme: CardThemeData(
        color: surfaceRaised,
        elevation: 0,
        margin: EdgeInsets.zero,
        clipBehavior: Clip.antiAlias,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(20),
          side: const BorderSide(color: border),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: surface,
        isDense: true,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        hintStyle: textTheme.bodyMedium?.copyWith(color: textMuted),
        prefixIconColor: textMuted,
        suffixIconColor: textMuted,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: accent, width: 2),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: accent,
          foregroundColor: onAccent,
          minimumSize: const Size(0, 50),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: textTheme.labelLarge,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: text,
          minimumSize: const Size(0, 48),
          side: const BorderSide(color: border),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: textTheme.labelLarge,
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: surface,
        selectedColor: accent.withValues(alpha: 0.16),
        side: const BorderSide(color: border),
        labelStyle: textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600),
        secondaryLabelStyle: textTheme.bodySmall?.copyWith(
          fontWeight: FontWeight.w700,
          color: accent,
        ),
        shape: const StadiumBorder(),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
      ),
      progressIndicatorTheme: const ProgressIndicatorThemeData(color: accent),
      dividerTheme: const DividerThemeData(
        color: border,
        space: 1,
        thickness: 1,
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: surfaceRaised,
        contentTextStyle: textTheme.bodyMedium?.copyWith(color: text),
        behavior: SnackBarBehavior.floating,
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: surfaceRaised,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
      ),
    );
  }
}
