import pygame
from ui_aux import font


class FloatingText:
    def __init__(self, text, x, y, color=(255, 255, 255), duration=60):
        self.text = text
        self.x = x
        self.y = y
        self.color = color
        self.duration = duration
        self.alpha = 255
        self.velocity = -1  # Move upward
        self.text_surface = font.render(text, True, color)
        self.text_rect = self.text_surface.get_rect(center=(x, y))

    def update(self):
        self.y += self.velocity
        self.alpha = max(0, self.alpha - (255 / self.duration))
        self.text_rect.center = (self.x, self.y)
        return self.alpha > 0

    def draw(self, screen):
        if self.alpha > 0:
            text_surface = font.render(self.text, True, self.color)
            text_surface.set_alpha(int(self.alpha))
            screen.blit(text_surface, self.text_rect)


class FloatingTextManager:
    def __init__(self):
        self.texts = []

    def add_text(self, text, x, y, color=(255, 255, 255), duration=60):
        self.texts.append(FloatingText(text, x, y, color, duration))

    def update(self):
        self.texts = [text for text in self.texts if text.update()]

    def draw(self, screen):
        for text in self.texts:
            text.draw(screen)
