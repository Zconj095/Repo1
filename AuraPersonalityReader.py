import random
from typing import Dict, List, Optional

class AuraPersonalityReader:
    def __init__(self):
        # Enhanced mapping with more detailed traits and attributes
        self.aura_traits_map = {
            "red": {
                "primary_traits": "Passionate, energetic, and competitive",
                "strengths": ["Leadership", "Courage", "Determination", "Action-oriented"],
                "challenges": ["Impatience", "Aggression", "Impulsiveness"],
                "element": "Fire",
                "chakra": "Root Chakra"
            },
            "orange": {
                "primary_traits": "Creative, adventurous, and confident",
                "strengths": ["Creativity", "Enthusiasm", "Social skills", "Adaptability"],
                "challenges": ["Restlessness", "Scattered focus", "Attention-seeking"],
                "element": "Fire/Water",
                "chakra": "Sacral Chakra"
            },
            "yellow": {
                "primary_traits": "Optimistic, cheerful, and intellectual",
                "strengths": ["Intelligence", "Clarity", "Positivity", "Learning ability"],
                "challenges": ["Over-thinking", "Criticism", "Perfectionism"],
                "element": "Air",
                "chakra": "Solar Plexus Chakra"
            },
            "green": {
                "primary_traits": "Balanced, natural, and stable",
                "strengths": ["Healing", "Growth", "Harmony", "Compassion"],
                "challenges": ["Possessiveness", "Envy", "Stubbornness"],
                "element": "Earth",
                "chakra": "Heart Chakra"
            },
            "blue": {
                "primary_traits": "Calm, trustworthy, and communicative",
                "strengths": ["Communication", "Truth", "Reliability", "Peace"],
                "challenges": ["Sadness", "Rigidity", "Over-sensitivity"],
                "element": "Water",
                "chakra": "Throat Chakra"
            },
            "indigo": {
                "primary_traits": "Intuitive, curious, and reflective",
                "strengths": ["Intuition", "Wisdom", "Spiritual insight", "Deep thinking"],
                "challenges": ["Isolation", "Depression", "Overthinking"],
                "element": "Light",
                "chakra": "Third Eye Chakra"
            },
            "violet": {
                "primary_traits": "Imaginative, visionary, and sensitive",
                "strengths": ["Spirituality", "Imagination", "Transformation", "Higher consciousness"],
                "challenges": ["Impracticality", "Detachment", "Moodiness"],
                "element": "Thought",
                "chakra": "Crown Chakra"
            },
            "pink": {
                "primary_traits": "Loving, nurturing, and compassionate",
                "strengths": ["Love", "Kindness", "Empathy", "Healing"],
                "challenges": ["Over-sensitivity", "Naivety", "Emotional dependency"],
                "element": "Heart",
                "chakra": "Heart Chakra"
            },
            "white": {
                "primary_traits": "Pure, spiritual, and enlightened",
                "strengths": ["Purity", "Clarity", "Spiritual connection", "Balance"],
                "challenges": ["Perfectionism", "Isolation", "Impracticality"],
                "element": "Spirit",
                "chakra": "Crown Chakra"
            },
            "black": {
                "primary_traits": "Protective, mysterious, and introspective",
                "strengths": ["Protection", "Mystery", "Depth", "Transformation"],
                "challenges": ["Negativity", "Depression", "Isolation"],
                "element": "Void",
                "chakra": "Root Chakra"
            }
        }

    def get_available_colors(self) -> List[str]:
        """Return list of available aura colors."""
        return list(self.aura_traits_map.keys())

    def read_aura(self, aura_color: str) -> Optional[Dict]:
        """Return detailed information about the aura color."""
        return self.aura_traits_map.get(aura_color.lower())

    def display_detailed_traits(self, aura_color: str) -> None:
        """Display detailed personality analysis based on aura color."""
        aura_data = self.read_aura(aura_color)
        
        if not aura_data:
            print(f"Unknown aura color: '{aura_color}'. Available colors: {', '.join(self.get_available_colors())}")
            return

        print(f"\n{'='*50}")
        print(f"AURA READING FOR: {aura_color.upper()}")
        print(f"{'='*50}")
        print(f"Primary Traits: {aura_data['primary_traits']}")
        print(f"\nStrengths:")
        for strength in aura_data['strengths']:
            print(f"  • {strength}")
        print(f"\nChallenges to work on:")
        for challenge in aura_data['challenges']:
            print(f"  • {challenge}")
        print(f"\nElement: {aura_data['element']}")
        print(f"Associated Chakra: {aura_data['chakra']}")
        print(f"{'='*50}\n")

    def generate_daily_insight(self, aura_color: str) -> str:
        """Generate a random daily insight based on aura color."""
        aura_data = self.read_aura(aura_color)
        if not aura_data:
            return "Focus on discovering your true colors today."

        insights = [
            f"Today, embrace your {aura_data['element'].lower()} energy and let it guide you.",
            f"Channel your {random.choice(aura_data['strengths']).lower()} to overcome challenges.",
            f"Be mindful of {random.choice(aura_data['challenges']).lower()} - turn it into growth.",
            f"Your {aura_data['chakra']} is calling for attention today.",
            f"Trust your {aura_color} aura's wisdom in decisions today."
        ]
        return random.choice(insights)

    def compatibility_check(self, color1: str, color2: str) -> str:
        """Check compatibility between two aura colors."""
        compatible_pairs = {
            ("red", "yellow"): "High energy match - dynamic and powerful",
            ("blue", "green"): "Harmonious balance - peace and growth",
            ("violet", "indigo"): "Spiritual connection - deep understanding",
            ("orange", "red"): "Creative fire - passionate innovation",
            ("green", "pink"): "Loving harmony - healing and nurturing",
            ("white", "violet"): "Pure spirituality - enlightened connection"
        }
        
        pair = tuple(sorted([color1.lower(), color2.lower()]))
        return compatible_pairs.get(pair, "Unique combination - explore the balance together")

def main():
    aura_reader = AuraPersonalityReader()
    
    print("🌈 Welcome to the Enhanced Aura Personality Reader! 🌈")
    print(f"Available colors: {', '.join(aura_reader.get_available_colors())}")
    
    while True:
        print("\nChoose an option:")
        print("1. Get detailed aura reading")
        print("2. Get daily insight")
        print("3. Check compatibility")
        print("4. Exit")
        
        choice = input("\nEnter your choice (1-4): ").strip()
        
        if choice == "1":
            aura_color = input("Enter your aura color: ").strip()
            aura_reader.display_detailed_traits(aura_color)
        
        elif choice == "2":
            aura_color = input("Enter your aura color for daily insight: ").strip()
            insight = aura_reader.generate_daily_insight(aura_color)
            print(f"\n💫 Daily Insight: {insight}\n")
        
        elif choice == "3":
            color1 = input("Enter first aura color: ").strip()
            color2 = input("Enter second aura color: ").strip()
            compatibility = aura_reader.compatibility_check(color1, color2)
            print(f"\n💕 Compatibility: {compatibility}\n")
        
        elif choice == "4":
            print("Thank you for using the Aura Personality Reader! ✨")
            break
        
        else:
            print("Invalid choice. Please try again.")

if __name__ == "__main__":
    main()
